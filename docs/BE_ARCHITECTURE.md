# BE_ARCHITECTURE.md — Origami Community Platform

## References
- `BE_PROJECT_RULES.md` — coding rules & patterns
- `Origami_ERD_Design_v2.docx` — database schema (33 tables, IT1/IT2/IT3)

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                   NextJS SPA (Frontend)              │
└──────────────────────┬──────────────────────────────┘
                       │ HTTP REST  /api/v1/...
                       ▼
┌─────────────────────────────────────────────────────┐
│          OrigamiCommunity.API                        │
│  Controllers · Middlewares · Program.cs · DI wiring  │
└──────────────────────┬──────────────────────────────┘
                       │
                       ▼
┌─────────────────────────────────────────────────────┐
│          OrigamiCommunity.Application                │
│  Features (Services · DTOs · Validators) · AutoMapper│
│  Interfaces/Repositories · Common (ApiResponse, ...) │
└────────┬─────────────────────────┬──────────────────┘
         │                         │
         ▼                         ▼
┌────────────────┐   ┌─────────────────────────────────┐
│ OrigamiCom-    │   │   OrigamiCommunity.Infrastructure│
│ munity.Domain  │◄──│   EF Core · Repositories         │
│ Entities·Enums │   │   JwtService · FileStorageService│
│ Constants      │   │   Background Jobs                │
└────────────────┘   └──────────────┬────────────────-─┘
                                    │
                                    ▼
                          ┌──────────────────┐
                          │   SQL Server DB   │
                          │  (33 tables IT1-3)│
                          └──────────────────┘
```

**Dependency rule (Clean Architecture):**
```
API → Application → Domain        (inner layers have NO outward deps)
Infrastructure → Application + Domain
```

---

## 2. Solution Structure

```
backend/
└── OrigamiCommunity.sln
    │
    ├── OrigamiCommunity.API/
    │   ├── Controllers/
    │   │   ├── AuthController.cs
    │   │   ├── TutorialController.cs
    │   │   ├── ReviewController.cs
    │   │   ├── CommunityController.cs
    │   │   ├── ModerationController.cs
    │   │   ├── SubscriptionController.cs
    │   │   ├── FamilyProjectController.cs
    │   │   ├── AchievementController.cs
    │   │   ├── AdCampaignController.cs
    │   │   └── AdminController.cs
    │   ├── Middlewares/
    │   │   ├── ExceptionMiddleware.cs
    │   │   └── BlockedWordMiddleware.cs
    │   ├── Program.cs
    │   ├── appsettings.json
    │   ├── appsettings.Development.json
    │   └── DependencyInjection.cs
    │
    ├── OrigamiCommunity.Application/
    │   ├── Features/
    │   │   ├── Auth/                    # FT-01, FT-02
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Tutorials/               # FT-04, FT-05a/b/c, FT-06
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Community/               # FT-07, FT-08a, FT-09
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Moderation/              # FT-08b
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── VipSubscription/         # FT-10, FT-11, FT-12
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Payment/                 # FT-10 (transaction flow)
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Achievement/             # FT-13
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Journal/                 # FT-14
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── FamilyProject/           # FT-15, FT-16
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   ├── Advertisement/           # FT-17, FT-18
    │   │   │   ├── Services/
    │   │   │   ├── DTOs/
    │   │   │   └── Validators/
    │   │   └── AdminConfiguration/      # FT-03
    │   │       ├── Services/
    │   │       ├── DTOs/
    │   │       └── Validators/
    │   ├── Common/
    │   │   ├── ApiResponse.cs
    │   │   ├── PaginationHelper.cs
    │   │   ├── PaginatedResult.cs
    │   │   └── MappingProfiles/
    │   │       └── AutoMapperProfile.cs
    │   ├── Interfaces/
    │   │   └── Repositories/
    │   │       ├── IUserRepository.cs
    │   │       ├── ITutorialRepository.cs
    │   │       ├── ICommunityRepository.cs
    │   │       ├── ISubscriptionRepository.cs
    │   │       ├── IFamilyProjectRepository.cs
    │   │       ├── IAdRepository.cs
    │   │       ├── INotificationRepository.cs
    │   │       └── IAuditLogRepository.cs
    │   └── DependencyInjection.cs
    │
    ├── OrigamiCommunity.Domain/
    │   ├── Entities/
    │   │   ├── User.cs
    │   │   ├── UserProfile.cs
    │   │   ├── UserRole.cs
    │   │   ├── FollowRelationship.cs
    │   │   ├── Category.cs
    │   │   ├── Tutorial.cs
    │   │   ├── TutorialStep.cs
    │   │   ├── TutorialReviewHistory.cs  # IMMUTABLE — INSERT ONLY
    │   │   ├── CommunityPost.cs
    │   │   ├── CommunityPostMedia.cs
    │   │   ├── Comment.cs
    │   │   ├── Like.cs
    │   │   ├── Wishlist.cs
    │   │   ├── Notification.cs
    │   │   ├── Report.cs
    │   │   ├── AuditLog.cs
    │   │   ├── BlockedWord.cs
    │   │   ├── Transaction.cs
    │   │   ├── VipSubscription.cs
    │   │   ├── CreatorVipSettings.cs
    │   │   ├── Achievement.cs
    │   │   ├── Journal.cs
    │   │   ├── FamilySubscription.cs
    │   │   ├── FamilyProject.cs
    │   │   ├── FamilyProjectMember.cs
    │   │   ├── FamilyProjectStepProgress.cs
    │   │   ├── AdPlacement.cs
    │   │   ├── AdCampaign.cs
    │   │   ├── AdBanner.cs
    │   │   ├── AdImpression.cs
    │   │   ├── AdClick.cs
    │   │   └── EmailLog.cs
    │   ├── Enums/
    │   │   ├── TutorialStatus.cs
    │   │   ├── TutorialType.cs
    │   │   ├── AccountStatus.cs
    │   │   ├── UserRoleType.cs
    │   │   ├── TransactionStatus.cs
    │   │   ├── TargetType.cs
    │   │   ├── PricingType.cs
    │   │   ├── CampaignStatus.cs
    │   │   └── ProjectStatus.cs
    │   └── Constants/
    │       └── AppConstants.cs
    │
    ├── OrigamiCommunity.Infrastructure/
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs
    │   │   ├── Configurations/          # Fluent API per entity
    │   │   │   ├── TutorialConfiguration.cs
    │   │   │   ├── UserConfiguration.cs
    │   │   │   └── ...
    │   │   └── Migrations/
    │   ├── Repositories/
    │   │   ├── UserRepository.cs
    │   │   ├── TutorialRepository.cs
    │   │   ├── CommunityRepository.cs
    │   │   ├── SubscriptionRepository.cs
    │   │   ├── FamilyProjectRepository.cs
    │   │   ├── AdRepository.cs
    │   │   ├── NotificationRepository.cs
    │   │   └── AuditLogRepository.cs
    │   ├── Services/
    │   │   ├── JwtService.cs
    │   │   ├── EmailService.cs
    │   │   ├── FileStorageService.cs     # Cloudinary
    │   │   └── BlockedWordService.cs     # In-memory HashSet cache
    │   ├── BackgroundJobs/
    │   │   ├── SubscriptionExpiryJob.cs  # Daily — expire VIP & Family subs
    │   │   └── AdBudgetDepletionJob.cs   # Every 15 min — stop depleted ads
    │   └── DependencyInjection.cs
    │
    └── OrigamiCommunity.Tests/
        ├── Features/
        │   ├── Auth/
        │   ├── Tutorials/
        │   ├── Community/
        │   └── ...
        └── Controllers/
```

---

## 3. Layer Responsibilities

| Project | Role | Can depend on |
|---|---|---|
| `OrigamiCommunity.API` | Controllers, Middleware, Program.cs, DI wiring | Application only |
| `OrigamiCommunity.Application` | Features (Services, DTOs, Validators), AutoMapper, Repository interfaces | Domain only |
| `OrigamiCommunity.Domain` | Entities (33 tables), Enums, Constants — zero business logic | ❌ None |
| `OrigamiCommunity.Infrastructure` | EF Core, Repositories, JwtService, EmailService, Cloudinary, Background Jobs | Application + Domain |

---

## 4. Feature Anatomy (Application layer)

```
Application/Features/[FeatureName]/
├── Services/
│   ├── I[FeatureName]Service.cs    ← interface defined here (Application)
│   └── [FeatureName]Service.cs     ← implementation here (Application)
├── DTOs/
│   ├── [Action]Request.cs          ← input model + FluentValidation target
│   └── [Action]Response.cs         ← output model mapped via AutoMapper
└── Validators/
    └── [Action]RequestValidator.cs  ← FluentValidation AbstractValidator<T>
```

**Feature → FT mapping:**

| Feature folder | FT covered |
|---|---|
| `Auth` | FT-01, FT-02 |
| `Tutorials` | FT-04, FT-05a (Manager approval), FT-05b (Search), FT-05c (Edit-after-publish), FT-06 (SEO) |
| `Community` | FT-07, FT-08a (Social), FT-09 (Follow feed) |
| `Moderation` | FT-08b (Account enforcement, comment moderation) |
| `VipSubscription` | FT-10, FT-11, FT-12 |
| `Payment` | FT-10 (Transaction confirm flow) |
| `Achievement` | FT-13 |
| `Journal` | FT-14 |
| `FamilyProject` | FT-15, FT-16 |
| `Advertisement` | FT-17, FT-18 |
| `AdminConfiguration` | FT-03 |

---

## 5. Request Flow

```
HTTP Request
  └─► API Controller          (routing, [Authorize], calls service)
        └─► Application Service  (business logic, BR validation, AutoMapper)
              └─► Repository Interface  (defined in Application/Interfaces/)
                    └─► Infrastructure Repository  (EF Core → SQL Server)
                          └─► Domain Entity
  ◄─── ApiResponse<T>  (all endpoints return this wrapper)
```

**Important:** `BlockedWordMiddleware` intercepts POST/PUT/PATCH requests **before** they reach the controller and rejects content containing blocked words (BR-23, BR-24).

---

## 6. Background Jobs

Two `IHostedService` implementations run automatically:

| Job | Schedule | What it does |
|---|---|---|
| `SubscriptionExpiryJob` | Daily at 02:00 UTC | Finds `VipSubscriptions` and `FamilySubscriptions` where `EndDate < UtcNow && Status = Active` → sets `Status = Expired` |
| `AdBudgetDepletionJob` | Every 15 minutes | Finds `AdCampaigns` where `BudgetRemaining ≤ 0 && Status = Active` → sets `Status = Ended`, removes banners |

---

## 7. Cross-Feature Communication

**Allowed:**
- Inject another feature's `IService` interface via constructor DI
- Domain events / notification triggers

**Forbidden:**
- Direct class-to-class references between feature folders
- Bypassing service layer to call a repository from another feature
- Referencing `Infrastructure` from `Application` or `API`

---

## 8. Configuration

```
API/appsettings.json
├── ConnectionStrings.Default       → SQL Server connection
├── Jwt.Key / Issuer / Audience / ExpiryMinutes
├── Email.SmtpHost / SmtpPort / From
└── Cloudinary.CloudName / ApiKey / ApiSecret
```

- Secrets: `.NET Secret Manager` (dev) or environment variables (prod)
- Strongly typed: `IOptions<JwtSettings>`, `IOptions<EmailSettings>`, etc.
- Never commit `appsettings.Development.json` — add to `.gitignore`

---

## 9. Tech Stack Summary

| Concern | Choice |
|---|---|
| Language | C# (.NET 8) |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (Code First) |
| Validation | FluentValidation |
| Mapping | AutoMapper |
| Auth | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Email | MailKit |
| File storage | Cloudinary SDK |
| Password hashing | BCrypt.Net-Next |
| Background jobs | `IHostedService` (built-in .NET) |
| Testing | xUnit + Moq |
| API docs | Swagger / Swashbuckle |
