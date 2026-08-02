# BE_PROJECT_RULES.md — OriMate (Origami Community Platform)

## References
- `BE_ARCHITECTURE.md` — solution structure & layer diagram
- `CLAUDE.md` — quy tắc dành cho AI coding agent (đồng bộ với file này)
- `docs/FT_MAPPING_v5.md`, `docs/MVP_SCOPE.md`
- `Origami_ERD_Design_v3.docx` — database schema

---

## 1. Tech Stack

| Concern | Choice | Version |
|---|---|---|
| Language | C# | .NET 8 |
| Framework | ASP.NET Core Web API | .NET 8 |
| ORM | Entity Framework Core | Code First |
| Pattern | Command/Query + Handler (CQRS-lite) | — |
| Validation | FluentValidation | chỉ dùng khi validate phức tạp — xem mục 5.5 |
| Auth | JWT Bearer | built-in .NET 8 |
| Password | BCrypt qua `IPasswordHasher` | latest |
| Email | MailKit (Gmail SMTP) | latest |
| File storage | Cloudinary SDK | latest |
| Background jobs | `IHostedService` | built-in .NET 8 |
| Testing | xUnit + Moq | latest |
| API docs | Swashbuckle (Swagger) | latest |

---

## 2. Naming Conventions

| Item | Convention | Ví dụ |
|---|---|---|
| Feature folder | PascalCase | `Tutorials`, `Achievements`, `Subscriptions` |
| Controller | `[Feature]Controller.cs` | `TutorialController.cs` |
| Command | `VerbNounCommand` | `CreateAchievementCommand`, `SubmitTutorialCommand` |
| Query | `GetNounQuery` | `GetTutorialsQuery`, `GetUserAchievementsQuery` |
| Handler | `VerbNounHandler` / `GetNounHandler` | `CreateAchievementHandler`, `GetTutorialsHandler` |
| Repository interface/class | `I[Entity]Repository.cs` / `[Entity]Repository.cs` | `ITutorialRepository.cs` |
| DTO output | `NounDto` | `AchievementDto.cs`, `TutorialDetailDto.cs` |
| DTO input (khi cần validate riêng) | `NounRequest` | `CreateTutorialRequest.cs` |
| Validator | `[Action]RequestValidator.cs` | `LoginRequestValidator.cs` |
| Classes / Methods | PascalCase | `HandleAsync`, `GetBySlugAsync` |
| Variables / Params | camelCase | `tutorialId`, `currentUserId` |
| Constants | UPPER_SNAKE_CASE hoặc `const int MaxNoteLength` (PascalCase chấp nhận được nếu private trong Handler) | `MaxNoteLength` |
| Interfaces | Prefix `I` | `IAuthService`, `ITutorialRepository` |
| Không viết tắt | — | `CreateVipSubscriptionCommand`, không viết `CreateVipSubCommand` |

---

## 3. Layer & Project Rules

### Dependency direction (KHÔNG BAO GIỜ vi phạm)
```
API → Application → Domain
Infrastructure → Application → Domain

API không được reference Infrastructure hay Domain trực tiếp (trừ DI wiring ở Program.cs).
Application không được reference Infrastructure (dùng interface).
Domain không reference project nào khác.
```

### Trách nhiệm từng project

**OrigamiPlatform.Domain** — entities, enums, exceptions:
- Entity class (property thuần C#, navigation property)
- Enum (`TutorialStatus`, `AccountStatus`...)
- Exception (`DomainException`, `NotFoundException`, `ForbiddenException`)

**OrigamiPlatform.Application** — business logic sống ở đây:
- `Commands/[Feature]/[Action]Command.cs` + `[Action]Handler.cs`
- `Queries/[Feature]/[Action]Query.cs` + `[Action]Handler.cs`
- `DTOs/[Feature]/` — output DTO, và Request DTO nếu cần validate riêng
- `Validators/[Feature]/` — chỉ khi cần (xem mục 5.5)
- `Interfaces/` — repository interfaces (flat, không lồng theo feature)

**OrigamiPlatform.Infrastructure** — I/O only:
- EF Core `AppDbContext` + Fluent API configurations
- Repository implementations
- `JwtService`/`ITokenService`, `EmailService`, `FileStorageService`, `BlockedWordService`
- Background jobs (`SubscriptionExpiryJob`)

**OrigamiPlatform.API** — thin layer:
- Controllers — route, `[Authorize]`, khởi tạo Command/Query, gọi Handler, trả kết quả. Không business logic.
- `ExceptionMiddleware`, `BlockedWordMiddleware`
- `Program.cs`, DI wiring

---

## 4. Feature Structure

```
Application/Commands/[FeatureName]/
├── [Action]Command.cs
└── [Action]Handler.cs

Application/Queries/[FeatureName]/
├── [Action]Query.cs
└── [Action]Handler.cs
```
Controller tương ứng ở `API/Controllers/[FeatureName]Controller.cs`, inject thẳng từng Handler cần dùng — **không qua Service trung gian**.

---

## 5. Code Patterns — MUST follow

### 5.1 Command + Handler

```csharp
// Application/Commands/Tutorials/SubmitTutorialCommand.cs
public record SubmitTutorialCommand(Guid TutorialId, Guid AuthorId);

// Application/Commands/Tutorials/SubmitTutorialHandler.cs
public class SubmitTutorialHandler
{
    private readonly ITutorialRepository _repo;

    public SubmitTutorialHandler(ITutorialRepository repo) => _repo = repo;

    public async Task<TutorialDto> HandleAsync(SubmitTutorialCommand cmd, CancellationToken ct = default)
    {
        var tutorial = await _repo.GetByIdAsync(cmd.TutorialId, ct)
            ?? throw new NotFoundException("Tutorial not found.");

        if (tutorial.AuthorId != cmd.AuthorId)
            throw new ForbiddenException("Only the author can submit this tutorial.");

        if (tutorial.Steps.Count is < 3 or > 30)
            throw new DomainException("Tutorial must have 3-30 steps. BR-TUT.");

        tutorial.Status = TutorialStatus.PendingManagerReview;
        await _repo.UpdateAsync(tutorial, ct);

        return tutorial.ToDto();
    }
}
```

### 5.2 Query + Handler

```csharp
public record GetTutorialsQuery(string? Keyword, Guid? CategoryId, int Page, int PageSize);

public class GetTutorialsHandler
{
    private readonly ITutorialRepository _repo;
    public GetTutorialsHandler(ITutorialRepository repo) => _repo = repo;

    public async Task<PagedResult<TutorialListItemDto>> HandleAsync(
        GetTutorialsQuery query, CancellationToken ct = default)
    {
        var result = await _repo.SearchAsync(query.Keyword, query.CategoryId, query.Page, query.PageSize, ct);
        return result.ToPagedDto();
    }
}
```

### 5.3 Controller — thin, inject thẳng Handler

```csharp
[ApiController]
[Route("api/[controller]")]
public class TutorialController : ControllerBase
{
    private readonly SubmitTutorialHandler _submitHandler;
    private readonly GetTutorialsHandler _getHandler;

    public TutorialController(SubmitTutorialHandler submitHandler, GetTutorialsHandler getHandler)
        => (_submitHandler, _getHandler) = (submitHandler, getHandler);

    [HttpPost("{id}/submit")]
    [Authorize]
    public async Task<IActionResult> Submit(Guid id)
    {
        var dto = await _submitHandler.HandleAsync(new SubmitTutorialCommand(id, GetCurrentUserId()));
        return Ok(dto);
    }

    private Guid GetCurrentUserId()
        => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
```

### 5.4 Repository interface (Application) + implementation (Infrastructure)

```csharp
// Application/Interfaces/ITutorialRepository.cs
public interface ITutorialRepository
{
    Task<Tutorial?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Tutorial?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<PagedResult<Tutorial>> SearchAsync(string? keyword, Guid? categoryId, int page, int pageSize, CancellationToken ct = default);
    Task AddAsync(Tutorial tutorial, CancellationToken ct = default);
    Task UpdateAsync(Tutorial tutorial, CancellationToken ct = default);
}

// Infrastructure/Repositories/TutorialRepository.cs
public class TutorialRepository : ITutorialRepository
{
    private readonly AppDbContext _context;
    public TutorialRepository(AppDbContext context) => _context = context;

    public async Task<Tutorial?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Tutorials.FindAsync([id], ct);

    public async Task AddAsync(Tutorial tutorial, CancellationToken ct = default)
    {
        _context.Tutorials.Add(tutorial);
        await _context.SaveChangesAsync(ct);
    }
}
```

### 5.5 Validate — tay trong Handler, hay tách FluentValidation Validator?

**Mặc định: validate tay trong Handler**, như `CreateAchievementHandler`:
```csharp
private static void Validate(string? photoUrl, string? note)
{
    if (note is { Length: > MaxNoteLength })
        throw new DomainException($"Note must not exceed {MaxNoteLength} characters.");
}
```

**Tách FluentValidation Validator riêng khi:** validate có nhiều rule phức tạp, dùng chung cho nhiều action, hoặc cần compose rule (xem mẫu ở `Auth` — `LoginRequestValidator`, `ChangePasswordRequestValidator`; hoặc `AdminConfiguration`). Validator nhận Command/Request làm target, được gọi ở đầu Handler hoặc qua pipeline riêng nếu về sau team quyết định thêm.

Không có quy tắc cứng "mọi Command phải có Validator" — quyết định theo độ phức tạp thực tế, tránh tạo file thừa cho validate 1-2 dòng.

### 5.6 Exception — không return null để báo lỗi

```csharp
// ✅ Đúng
var tutorial = await _repo.GetByIdAsync(id, ct)
    ?? throw new NotFoundException("Tutorial not found.");

// ❌ Sai — Controller phải tự check null, dễ quên
var tutorial = await _repo.GetByIdAsync(id, ct);
if (tutorial == null) return NotFound();
```
`ExceptionMiddleware` map: `DomainException` → 400, `NotFoundException` → 404, `ForbiddenException` → 403, unhandled → 500.

### 5.7 Async — mọi method phải async

```csharp
// ✅ Đúng
public async Task<TutorialDto> HandleAsync(SubmitTutorialCommand cmd, CancellationToken ct = default) { ... }

// ❌ Sai — không block bằng .Result hoặc .Wait()
public TutorialDto Handle(SubmitTutorialCommand cmd) { ... }
```

### 5.8 Authorization — dùng role-based attribute

```csharp
[Authorize]                                          // bất kỳ user đã đăng nhập
[Authorize(Roles = "Admin")]
[Authorize(Roles = "Manager")]
[Authorize(Roles = "Admin,Manager")]
[Authorize(Roles = "ContributorReviewer")]           // CTV — KHÔNG dùng cho duyệt tutorial, chỉ Weekly Challenge/comment moderation
[AllowAnonymous]
```

---

## 6. Anti-Patterns — KHÔNG BAO GIỜ làm

| ❌ Việc | Vì sao | ✅ Thay bằng |
|---|---|---|
| `AppDbContext` trong Handler | Vi phạm layer rule | Dùng `IRepository` interface |
| Business logic trong Controller | Controller phải thin | Chuyển vào Handler |
| Query dữ liệu ngoài `Infrastructure/Repositories/` | Data access không kiểm soát được | Dùng Repository pattern |
| Hardcode connection string/secret | Rủi ro bảo mật | `appsettings.json` + Secret Manager |
| `_context.SaveChangesAsync()` trong Handler | Bỏ qua Repository | Gọi qua Repository method |
| Gọi thẳng Repository của feature khác | Cross-feature coupling | Gọi qua Handler của feature đó |
| Trả raw entity từ Controller | Lộ DB schema | Luôn map qua DTO (`ToDto()` extension) |
| Tạo mới `TutorialService`/`AdminConfigService` kiểu Service pattern | Đã quyết định bỏ, đang refactor sang Handler | Command/Handler |
| Tạo lại code `FamilyProject*`/`Ad*` | Đã bị xoá khỏi scope (Hội đồng yêu cầu) | Không làm, xem `docs/MVP_SCOPE.md` |

---

## 7. Dependency Injection Setup

```csharp
// Application/DependencyInjection.cs
public static IServiceCollection AddApplication(this IServiceCollection services)
{
    // Đăng ký từng Handler — KHÔNG đăng ký Service tổng
    services.AddScoped<SubmitTutorialHandler>();
    services.AddScoped<GetTutorialsHandler>();
    services.AddScoped<CreateAchievementHandler>();
    services.AddScoped<GetUserAchievementsHandler>();
    // ... tất cả Handler khác, theo từng feature

    services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly()); // chỉ pick up Validator đã có
    return services;
}

// Infrastructure/DependencyInjection.cs
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services, IConfiguration config)
{
    services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(config.GetConnectionString("Default")));

    services.AddScoped<ITutorialRepository, TutorialRepository>();
    services.AddScoped<IAchievementRepository, AchievementRepository>();
    // ... tất cả Repository khác

    services.AddSingleton<IBlockedWordService, BlockedWordService>();
    services.AddScoped<ITokenService, TokenService>();
    services.AddScoped<IEmailService, EmailService>();
    services.AddScoped<IFileStorageService, FileStorageService>();

    services.AddHostedService<SubscriptionExpiryJob>();
    return services;
}

// API/Program.cs
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
```

---

## 8. EF Core — Code First Rules

- Entity class ở `Domain/Entities/`; Fluent API configuration ở `Infrastructure/Persistence/Configurations/`
- Dùng `ApplyConfigurationsFromAssembly` trong `AppDbContext` — không gọi `entity.Property(...)` trực tiếp trong `OnModelCreating`
- PK là `Guid`, `ValueGeneratedNever()` — generate ở application layer
- Mọi string field có `HasMaxLength(n)` — không để `nvarchar(max)` ngoài ý muốn
- **`TutorialReviewHistory` IMMUTABLE** — không cấu hình update convention, không bao giờ gọi `Update()`
- **`AuditLog` IMMUTABLE** — tương tự

**Quy tắc migration:**
- Chỉ 1 người tạo migration/lần — ping team trước
- Commit format: `chore: add migration [MigrationName]`
- Không sửa tay migration đã apply
- Migration xoá bảng `FamilyProject*`/`Ad*` phải tạo mới, KHÔNG sửa migration cũ đã tạo các bảng đó

---

## 9. Git Workflow

```
Branch naming:
  feature/FT-XX-short-description     vd: feature/FT-16-vip-subscription
  hotfix/short-description            vd: hotfix/fix-vip-expiry
  chore/short-description             vd: chore/refactor-tutorials-to-handler

Commit message:
  FT-XX: mô tả ngắn                   vd: FT-16: implement VIP subscription commands
  HOTFIX: mô tả ngắn
  chore: mô tả ngắn                   vd: chore: remove FamilyProject module

Pull Request:
  - Tối thiểu 1 reviewer (ưu tiên người phụ trách FT liên quan)
  - Gắn FT ID trong title
  - Build phải pass trước khi merge
```

**Phân công (theo `docs/MVP_SCOPE.md` — cập nhật theo tuần, không cố định như bảng cũ):**

| Thành viên | Tuần 1 | Tuần 2 | Tuần 3 |
|---|---|---|---|
| BE1 | Refactor Tutorials → Command/Handler | FT-10 Stuck button, FT-11 (nếu kịp) | Buffer/test |
| BE2 | Refactor AdminConfiguration → Command/Handler | FT-14 Moderation CTV, hỗ trợ Should-have | Buffer/test |
| BE3 | Xoá FamilyProjects + AdCampaigns/Ads | FT-16/17 VIP Subscription | FT-18 Shop (nếu kịp), buffer/test |

Khi sửa file thuộc feature người khác phụ trách → tạo PR, không push thẳng.

---

## 10. Testing

```
OrigamiPlatform.Tests/
├── Commands/
│   ├── Auth/
│   │   └── LoginHandlerTests.cs
│   ├── Tutorials/
│   │   └── SubmitTutorialHandlerTests.cs
│   └── ...
├── Queries/
└── Controllers/
```

**Quy tắc:**
- Framework: xUnit, Mock: Moq, Pattern: Arrange/Act/Assert
- Test tối thiểu cho Handler xử lý business rule (BR-*) — không cần 80% coverage cứng nhắc trong 3 tuần, ưu tiên test đúng các BR quan trọng (VIP gating, tutorial review, blocked word)
- Tên file test mirror theo Handler nó test

```csharp
[Fact]
public async Task SubmitTutorial_LessThan3Steps_ThrowsDomainException()
{
    // Arrange
    var repo = new Mock<ITutorialRepository>();
    repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), default))
        .ReturnsAsync(new Tutorial { Steps = new List<TutorialStep>() });
    var handler = new SubmitTutorialHandler(repo.Object);

    // Act & Assert
    await Assert.ThrowsAsync<DomainException>(
        () => handler.HandleAsync(new SubmitTutorialCommand(Guid.NewGuid(), Guid.NewGuid())));
}
```

---

## 11. Definition of Done — theo từng FT

Một FT được coi là **done** khi TẤT CẢ đúng:

- [ ] Repository interface định nghĩa ở `Application/Interfaces/` (nếu cần entity/repo mới)
- [ ] Command/Query + Handler viết đầy đủ, đúng naming convention
- [ ] DTO output + Request (nếu cần) đầy đủ
- [ ] Validate đầy đủ AC + NAC + BV của FT — tay trong Handler hoặc Validator riêng tuỳ độ phức tạp
- [ ] Controller action thin — không business logic
- [ ] DI đăng ký ở `DependencyInjection.cs`
- [ ] Toàn bộ AC pass qua Postman/Swagger
- [ ] Toàn bộ NAC trả đúng status code + message lỗi
- [ ] FE đã test endpoint, không báo bug trong 24h
- [ ] Build không warning
