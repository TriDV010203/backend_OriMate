# BE_PROJECT_RULES.md — Origami Community Platform

## References
- `BE_ARCHITECTURE.md` — solution structure & layer diagram
- `Origami_ERD_Design_v2.docx` — database schema

---

## 1. Tech Stack

| Concern | Choice | Version |
|---|---|---|
| Language | C# | .NET 8 |
| Framework | ASP.NET Core Web API | .NET 8 |
| ORM | Entity Framework Core | Code First |
| Validation | FluentValidation | latest stable |
| Mapping | AutoMapper | latest stable |
| Auth | JWT Bearer | built-in .NET 8 |
| Password | BCrypt.Net-Next | latest |
| Email | MailKit | latest |
| File storage | Cloudinary SDK | latest |
| Background jobs | `IHostedService` | built-in .NET 8 |
| Testing | xUnit + Moq | latest |
| API docs | Swashbuckle (Swagger) | latest |

---

## 2. Naming Conventions

| Item | Convention | Example |
|---|---|---|
| Feature folders | PascalCase | `Tutorials`, `FamilyProject` |
| Controllers | `[Feature]Controller.cs` | `TutorialController.cs` |
| Service interface | `I[Feature]Service.cs` | `ITutorialService.cs` |
| Service class | `[Feature]Service.cs` | `TutorialService.cs` |
| Repository interface | `I[Entity]Repository.cs` | `ITutorialRepository.cs` |
| Repository class | `[Entity]Repository.cs` | `TutorialRepository.cs` |
| DTO input | `[Action]Request.cs` | `CreateTutorialRequest.cs` |
| DTO output | `[Action]Response.cs` | `TutorialDetailResponse.cs` |
| Validator | `[Action]RequestValidator.cs` | `CreateTutorialRequestValidator.cs` |
| Classes / Methods | PascalCase | `GetBySlugAsync` |
| Variables / Params | camelCase | `tutorialId`, `currentUserId` |
| Constants | UPPER_SNAKE_CASE | `MAX_STEP_COUNT` |
| Interfaces | Prefix `I` | `IAuthService` |

---

## 3. Layer & Project Rules

### Dependency direction (NEVER violate)

```
API → Application → Domain
Infrastructure → Application + Domain

API must NOT reference Infrastructure or Domain directly.
Application must NOT reference Infrastructure (use interfaces).
Domain must NOT reference any other project.
```

### Responsibility per project

**OrigamiCommunity.Domain** — zero dependencies, zero business logic:
- Entity classes (plain C# properties, navigation properties)
- Enums (`TutorialStatus`, `AccountStatus`, etc.)
- Constants (`AppConstants.cs`)

**OrigamiCommunity.Application** — business logic lives here:
- Feature Services (implement business rules, BRs)
- DTOs (Request / Response models)
- FluentValidation Validators
- Repository interfaces (`Application/Interfaces/Repositories/`)
- AutoMapper profiles (`Application/Common/MappingProfiles/`)
- Shared helpers (`ApiResponse<T>`, `PaginationHelper`)

**OrigamiCommunity.Infrastructure** — I/O only:
- EF Core `AppDbContext` + Fluent API configurations
- Repository implementations (EF Core queries)
- `JwtService`, `EmailService`, `FileStorageService`, `BlockedWordService`
- Background jobs (`SubscriptionExpiryJob`, `AdBudgetDepletionJob`)

**OrigamiCommunity.API** — thin layer:
- Controllers (no business logic — route, authorize, call service, return response)
- `ExceptionMiddleware`, `BlockedWordMiddleware`
- `Program.cs`, DI wiring

---

## 4. Feature Structure

Every feature follows this layout inside `Application/Features/[FeatureName]/`:

```
[FeatureName]/
├── Services/
│   ├── I[FeatureName]Service.cs
│   └── [FeatureName]Service.cs
├── DTOs/
│   ├── [Action]Request.cs
│   └── [Action]Response.cs
└── Validators/
    └── [Action]RequestValidator.cs
```

Its controller lives in `API/Controllers/[FeatureName]Controller.cs`.

---

## 5. Code Patterns — MUST follow

### 5.1 ApiResponse wrapper

All endpoints return `ApiResponse<T>`. Never return raw objects.

```csharp
// Application/Common/ApiResponse.cs
public class ApiResponse<T>
{
    public bool IsSuccess { get; init; }
    public T? Data { get; init; }
    public string? Message { get; init; }
    public string? Error { get; init; }

    public static ApiResponse<T> Success(T data, string? message = null)
        => new() { IsSuccess = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string error)
        => new() { IsSuccess = false, Error = error };
}
```

### 5.2 Controller — thin, no business logic

```csharp
// API/Controllers/TutorialController.cs
[ApiController]
[Route("api/v1/[controller]")]
public class TutorialController : ControllerBase
{
    private readonly ITutorialService _service;
    public TutorialController(ITutorialService service) => _service = service;

    [HttpPost]
    [Authorize]                              // any authenticated user
    public async Task<IActionResult> Create([FromBody] CreateTutorialRequest req)
    {
        var result = await _service.CreateAsync(req, GetCurrentUserId());
        return result.IsSuccess ? Ok(result) : BadRequest(result);
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

### 5.3 Service — business logic & BR enforcement

```csharp
// Application/Features/Tutorials/Services/TutorialService.cs
public class TutorialService : ITutorialService
{
    private readonly ITutorialRepository _repo;
    private readonly IMapper _mapper;

    public TutorialService(ITutorialRepository repo, IMapper mapper)
        => (_repo, _mapper) = (repo, mapper);

    public async Task<ApiResponse<TutorialResponse>> CreateAsync(
        CreateTutorialRequest req, Guid authorId)
    {
        // Business rule enforcement (BR-12: title 5-150 chars etc.) done
        // by FluentValidation before this method is called.
        var tutorial = _mapper.Map<Tutorial>(req);
        tutorial.AuthorId = authorId;
        tutorial.Status = TutorialStatus.Draft;
        tutorial.CreatedAt = DateTime.UtcNow;

        await _repo.AddAsync(tutorial);
        return ApiResponse<TutorialResponse>.Success(_mapper.Map<TutorialResponse>(tutorial));
    }
}
```

### 5.4 Repository interface (Application) + implementation (Infrastructure)

```csharp
// Application/Interfaces/Repositories/ITutorialRepository.cs
public interface ITutorialRepository
{
    Task<Tutorial?> GetByIdAsync(Guid id);
    Task<Tutorial?> GetBySlugAsync(string slug);
    Task<PaginatedResult<Tutorial>> SearchAsync(string? keyword, int? categoryId,
        string? difficulty, string? type, int page, int pageSize);
    Task AddAsync(Tutorial tutorial);
    Task UpdateAsync(Tutorial tutorial);
}

// Infrastructure/Repositories/TutorialRepository.cs
public class TutorialRepository : ITutorialRepository
{
    private readonly AppDbContext _context;
    public TutorialRepository(AppDbContext context) => _context = context;

    public async Task<Tutorial?> GetByIdAsync(Guid id)
        => await _context.Tutorials.FindAsync(id);

    public async Task AddAsync(Tutorial tutorial)
    {
        _context.Tutorials.Add(tutorial);
        await _context.SaveChangesAsync();
    }
    // ...
}
```

### 5.5 FluentValidation validator

```csharp
// Application/Features/Tutorials/Validators/CreateTutorialRequestValidator.cs
public class CreateTutorialRequestValidator : AbstractValidator<CreateTutorialRequest>
{
    public CreateTutorialRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(5).WithMessage("Title must be at least 5 characters. BR-12.")
            .MaximumLength(150).WithMessage("Title must not exceed 150 characters. BR-12.");

        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(20).WithMessage("Description must be at least 20 characters. BR-12.")
            .MaximumLength(500);

        RuleFor(x => x.Difficulty)
            .NotEmpty()
            .Must(d => new[] { "Beginner", "Intermediate", "Advanced" }.Contains(d));

        RuleFor(x => x.Steps)
            .NotNull()
            .Must(s => s.Count >= 3 && s.Count <= 30)
            .WithMessage("Tutorial must have 3–30 steps. BR-12.");
    }
}
```

### 5.6 Exception handling (ExceptionMiddleware)

Throw these custom exceptions anywhere in Application/Infrastructure — the middleware catches and converts them to HTTP responses:

```csharp
// Usage in Service
if (tutorial == null) throw new NotFoundException("Tutorial not found.");
if (!authorized)      throw new ForbiddenException("Access denied.");
if (duplicate)        throw new ConflictException("Slug already exists.");
if (invalid)          throw new BadRequestException("Invalid request.");

// ExceptionMiddleware maps:
// NotFoundException    → 404  { "error": "..." }
// ForbiddenException  → 403  { "error": "..." }
// ConflictException   → 409  { "error": "..." }
// BadRequestException → 400  { "error": "..." }
// Unhandled           → 500  { "error": "An unexpected error occurred." }
```

### 5.7 BlockedWordMiddleware

Applied globally on POST/PUT/PATCH. Checks request body text against `BlockedWordService` (in-memory HashSet loaded from DB). Rejects with **422** if a match is found. This enforces BR-23 and BR-24 automatically for all content writes.

```csharp
// BlockedWordService caches words in a HashSet — case-insensitive
if (_blockedWordService.ContainsBlockedWord(bodyText))
{
    context.Response.StatusCode = 422;
    await context.Response.WriteAsJsonAsync(
        ApiResponse<object>.Fail("Content contains prohibited language."));
    return;
}
```

### 5.8 Async — all methods must be async

```csharp
// ✅ Correct
public async Task<ApiResponse<TutorialResponse>> GetByIdAsync(Guid id) { ... }

// ❌ Wrong — never block with .Result or .Wait()
public ApiResponse<TutorialResponse> GetById(Guid id) { ... }
```

### 5.9 Authorization — use role-based attributes

```csharp
[Authorize]                                          // any authenticated user
[Authorize(Roles = "Admin")]                         // Admin only
[Authorize(Roles = "Manager")]                       // Manager only
[Authorize(Roles = "Admin,Manager")]                 // Admin or Manager
[Authorize(Roles = "ContributorReviewer")]           // CTV Reviewer only
[Authorize(Roles = "AdvertisingPartner")]            // Ad Partner only
[AllowAnonymous]                                     // public endpoint
```

---

## 6. Anti-Patterns — NEVER do these

| ❌ What | Why | ✅ Instead |
|---|---|---|
| `AppDbContext` in Application service | Violates layer rule | Use `IRepository` interface |
| Business logic in Controller | Controller should be thin | Move to Service |
| Data queries outside `Infrastructure/Repositories/` | Unpredictable data access | Use Repository pattern |
| Hardcoded connection strings or secrets | Security risk | Use `appsettings.json` + Secret Manager |
| `_context.SaveChangesAsync()` in Service | Bypasses Repository | Call via Repository method |
| Calling another feature's Repository directly | Cross-feature coupling | Inject the feature's `IService` interface |
| Circular feature dependencies | Breaks DI | Extract to `Application/Common/` |
| Returning raw entity from Controller | Exposes DB schema | Always map to Response DTO |
| Skipping FluentValidation | BR enforcement gaps | Add validator for every Request DTO |

---

## 7. Dependency Injection Setup

Each project registers its own services. All wired in `Program.cs`.

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    // Feature services
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<ITutorialService, TutorialService>();
    services.AddScoped<ICommunityService, CommunityService>();
    services.AddScoped<IModerationService, ModerationService>();
    services.AddScoped<IVipSubscriptionService, VipSubscriptionService>();
    services.AddScoped<IPaymentService, PaymentService>();
    services.AddScoped<IAchievementService, AchievementService>();
    services.AddScoped<IJournalService, JournalService>();
    services.AddScoped<IFamilyProjectService, FamilyProjectService>();
    services.AddScoped<IAdvertisementService, AdvertisementService>();
    services.AddScoped<IAdminConfigService, AdminConfigService>();

    // AutoMapper + FluentValidation
    services.AddAutoMapper(Assembly.GetExecutingAssembly());
    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
    return services;
}

// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("Default")));

    // Repositories
    services.AddScoped<IUserRepository, UserRepository>();
    services.AddScoped<ITutorialRepository, TutorialRepository>();
    services.AddScoped<ICommunityRepository, CommunityRepository>();
    services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
    services.AddScoped<IFamilyProjectRepository, FamilyProjectRepository>();
    services.AddScoped<IAdRepository, AdRepository>();
    services.AddScoped<INotificationRepository, NotificationRepository>();
    services.AddScoped<IAuditLogRepository, AuditLogRepository>();

    // External services
    services.AddSingleton<IBlockedWordService, BlockedWordService>();
    services.AddScoped<IJwtService, JwtService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IFileStorageService, FileStorageService>();

    // Background jobs
    services.AddHostedService<SubscriptionExpiryJob>();
    services.AddHostedService<AdBudgetDepletionJob>();
    return services;
}

// API/Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

---

## 8. EF Core — Code First Rules

- All entity classes live in `Domain/Entities/`
- All Fluent API configurations live in `Infrastructure/Persistence/Configurations/`
- Use `ApplyConfigurationsFromAssembly` in `AppDbContext` — do NOT call `entity.Property(...)` inside `AppDbContext.OnModelCreating` directly
- All Guid PKs use `ValueGeneratedNever()` — generated by application, not DB
- All string fields must have `HasMaxLength(n)` — never allow `nvarchar(max)` by accident
- **TutorialReviewHistories is IMMUTABLE** — configure with no update conventions; never call `Update()` on this entity

```csharp
// Infrastructure/Persistence/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}

// override SaveChangesAsync to auto-set UpdatedAt
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.State == EntityState.Modified
            && entry.Entity.GetType().GetProperty("UpdatedAt") != null)
        {
            entry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
        }
    }
    return await base.SaveChangesAsync(ct);
}
```

**Migration rules:**
- Only ONE person creates a migration at a time — ping the team first
- Commit format: `chore: add migration [MigrationName]`
- Never modify an applied migration file

---

## 9. Git Workflow

```
Branch naming:
  feature/FT-XX-short-description     e.g. feature/FT-01-auth
  hotfix/short-description            e.g. hotfix/fix-vip-expiry

Commit message:
  FT-XX: short description            e.g. FT-01: implement user registration
  HOTFIX: short description
  chore: description                  e.g. chore: add InitialCreate migration

Pull Request:
  - At least 1 reviewer (preferably the person who owns the related FT)
  - Linked to FT ID in title
  - Build must pass before merge
```

**Vertical slice ownership:**

| Member | Owns |
|---|---|
| A (strongest) | FT-01, FT-02, FT-03, FT-04, FT-05a, FT-05b, FT-05c, FT-06 |
| B | FT-07, FT-08a, FT-08b, FT-09, FT-10, FT-11, FT-12 |
| C | FT-13, FT-14, FT-15, FT-16, FT-17, FT-18 |

When editing a file owned by another member → create a PR, do not push directly.

---

## 10. Testing

Test project: `OrigamiCommunity.Tests/` (separate `.csproj` in the solution)

```
OrigamiCommunity.Tests/
├── Features/
│   ├── Auth/
│   │   └── AuthServiceTests.cs
│   ├── Tutorials/
│   │   └── TutorialServiceTests.cs
│   └── ...
└── Controllers/
    ├── AuthControllerTests.cs
    └── ...
```

**Rules:**
- Framework: xUnit
- Mocking: Moq
- Pattern: Arrange / Act / Assert
- Minimum coverage: 80% for Service layer
- Test file mirrors the feature it tests

```csharp
[Fact]
public async Task SubmitTutorial_LessThan3Steps_ReturnsBadRequest()
{
    // Arrange
    var request = new SubmitTutorialRequest
    {
        TutorialId = Guid.NewGuid(),
        // Steps not set — should fail BR-12
    };
    var validator = new SubmitTutorialRequestValidator();

    // Act
    var result = await validator.ValidateAsync(request);

    // Assert
    Assert.False(result.IsValid);
    Assert.Contains(result.Errors, e => e.PropertyName == "Steps");
}
```

---

## 11. Definition of Done — per FT

A feature is **done** when ALL of the following are true:

- [ ] Repository interface defined in `Application/Interfaces/Repositories/`
- [ ] Service interface and implementation complete
- [ ] All DTOs and Validators written (covering AC + NAC + BV from FT spec)
- [ ] Controller action thin — no business logic
- [ ] DI registered in `DependencyInjection.cs`
- [ ] All AC pass via Postman / Swagger test
- [ ] All NAC return correct error status and message
- [ ] Frontend has tested the endpoint and reported no bugs for 24 h
- [ ] No build warnings in the project
