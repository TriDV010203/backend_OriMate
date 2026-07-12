# BE_ARCHITECTURE.md — OriMate (Origami Community Platform)

## References
- `BE_PROJECT_RULES.md` — coding rules & patterns
- `CLAUDE.md` — quy tắc dành cho AI coding agent (nguồn tham chiếu chính, đồng bộ với file này)
- `docs/FT_MAPPING_v5.md` — mapping FT ↔ feature/entity/actor
- `docs/MVP_SCOPE.md` — phạm vi & trạng thái thực tế từng feature
- `Origami_ERD_Design_v3.docx` — database schema

⚠️ File này mô tả **kiến trúc thật của solution `OrigamiPlatform.*`**, đã đối chiếu trực tiếp với code (không còn suy diễn). Nếu thấy sai lệch với code, code là nguồn đúng — báo lại để cập nhật file.

---

## 1. High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│                   NextJS 14 SPA (Frontend)            │
└──────────────────────┬──────────────────────────────┘
                        │ HTTP REST  /api/...
                        ▼
┌─────────────────────────────────────────────────────┐
│          OrigamiPlatform.API                          │
│  Controllers · Middlewares · Program.cs · DI wiring   │
└──────────────────────┬──────────────────────────────┘
                        │
                        ▼
┌─────────────────────────────────────────────────────┐
│          OrigamiPlatform.Application                  │
│  Commands/ · Queries/ · Handlers · DTOs/ · Validators/ │
│  Interfaces/ (repository interfaces)                   │
└────────┬─────────────────────────┬──────────────────┘
         │                         │
         ▼                         ▼
┌────────────────┐   ┌─────────────────────────────────┐
│ OrigamiPlatform│   │   OrigamiPlatform.Infrastructure │
│ .Domain        │◄──│   EF Core · Repositories          │
│ Entities·Enums │   │   JwtService · EmailService       │
│ Exceptions     │   │   FileStorageService · Background │
└────────────────┘   │   Jobs                            │
                      └──────────────┬────────────────-─┘
                                     │
                                     ▼
                          ┌──────────────────┐
                          │   SQL Server DB   │
                          └──────────────────┘
```

**Dependency rule (Clean Architecture — bắt buộc):**
```
API → Application → Domain
Infrastructure → Application → Domain
API → Infrastructure (chỉ để đăng ký DI trong Program.cs)
```
Domain zero dependencies. Application không được reference Infrastructure hay API.

**Về CQRS:** solution dùng **Command/Query + Handler** (record Command/Query, class Handler xử lý qua `HandleAsync`), không dùng MediatR. Đây **không phải CQRS đầy đủ** theo định nghĩa gốc — Command và Query vẫn đọc/ghi chung 1 `AppDbContext`, không tách read model/write model, không tách database. Gọi chính xác là **"CQRS-lite"** hay **"Command/Handler pattern"** khi trình bày, tránh gây hiểu nhầm khi giảng viên/hội đồng hỏi sâu.

---

## 2. Solution Structure (thực tế, đã đối chiếu code)

```
backend/
└── OrigamiPlatform.sln
    │
    ├── OrigamiPlatform.API/
    │   ├── Controllers/
    │   ├── Middlewares/
    │   │   ├── ExceptionMiddleware.cs
    │   │   └── BlockedWordMiddleware.cs
    │   ├── Program.cs
    │   └── DependencyInjection.cs
    │
    ├── OrigamiPlatform.Application/
    │   ├── Commands/
    │   │   ├── Auth/                    # FT-01, FT-02 — ✅ Done
    │   │   ├── Achievements/            # FT-19 — ✅ Done
    │   │   ├── Comments/                # FT-13 — ✅ Done
    │   │   ├── CommunityPosts/          # FT-12 — ✅ Done
    │   │   ├── Likes/                   # FT-12 — ✅ Done
    │   │   ├── Follows/                 # FT-13 — ✅ Done
    │   │   ├── Notifications/           # FT-13 — ✅ Done
    │   │   ├── Reports/                 # FT-12/14 — ✅ Done
    │   │   ├── Wishlists/               # bonus, giữ lại — ✅ Done
    │   │   ├── TutorialProgress/        # FT-09 — ✅ Done
    │   │   ├── Users/                   # FT-15 — ✅ Done
    │   │   ├── Journals/                # FT-21 — ✅ code sẵn, KHÔNG đăng ký route (Won't-have)
    │   │   ├── Tutorials/               # FT-04/05/07 — 🔴 đang refactor từ Service, xem MVP_SCOPE.md mục 1
    │   │   ├── Moderation/              # FT-14 (CTV) — ⚪ chưa code
    │   │   └── Subscriptions/           # FT-16 — ⚪ chưa code, ưu tiên cao
    │   │
    │   ├── Queries/
    │   │   ├── Achievements/ Comments/ CommunityPosts/ FamilyProjects*/
    │   │   ├── Journals/ Notifications/ Reports/ TutorialProgress/
    │   │   ├── Tutorials/               # FT-06/08 — ✅ Done (GetTutorialsQuery, GetTutorialBySlugQuery)
    │   │   ├── Users/ Wishlists/
    │   │   └── Dashboard/               # FT-17 Creator dashboard — ⚪ chưa code
    │   │
    │   ├── DTOs/
    │   │   ├── [FeatureName]/[Noun]Dto.cs, [Action]Request.cs (theo action ghi)
    │   │   └── Common/PagedResult.cs, MessageResponse.cs
    │   │
    │   ├── Validators/
    │   │   └── [FeatureName]/[Action]RequestValidator.cs
    │   │       — CHỈ dùng khi validate phức tạp (xem mẫu ở Auth, AdminConfiguration).
    │   │         Validate đơn giản làm tay trong Handler (xem mẫu Achievements).
    │   │
    │   ├── Interfaces/
    │   │   └── I[Entity]Repository.cs, IEmailService.cs, ITokenService.cs,
    │   │       IPasswordHasher.cs, IBlockedWordService.cs, INotificationService.cs
    │   │       (flat — không lồng theo feature)
    │   │
    │   └── DependencyInjection.cs
    │
    ├── OrigamiPlatform.Domain/
    │   ├── Entities/            # xem Origami_ERD_Design_v3.docx — KHÔNG còn FamilyProject*/Ad*
    │   ├── Enums/
    │   └── Exceptions/
    │       ├── DomainException.cs
    │       ├── NotFoundException.cs
    │       └── ForbiddenException.cs
    │
    ├── OrigamiPlatform.Infrastructure/
    │   ├── Persistence/
    │   │   ├── AppDbContext.cs
    │   │   ├── Configurations/          # Fluent API per entity
    │   │   └── Migrations/
    │   ├── Repositories/
    │   ├── Services/
    │   │   ├── JwtService.cs / TokenService
    │   │   ├── EmailService.cs          # Gmail SMTP, MailKit/MimeKit
    │   │   ├── FileStorageService.cs    # Cloudinary
    │   │   └── BlockedWordService.cs
    │   ├── BackgroundJobs/
    │   │   └── SubscriptionExpiryJob.cs
    │   └── DependencyInjection.cs
    │
    └── OrigamiPlatform.Tests/
        ├── Commands/
        ├── Queries/
        └── Controllers/
```

**Đang refactor (xem `docs/MVP_SCOPE.md` mục 1) — sẽ biến mất khỏi cây trên khi xong:**
```
Application/Features/Tutorials/Services/         → chuyển vào Commands/Tutorials/
Application/Features/AdminConfiguration/Services/ → chuyển vào Commands/AdminConfiguration/
```

**Đã xoá (không còn trong code, không tạo lại):**
```
Commands/FamilyProjects/, Queries liên quan, DTOs/FamilyProjects/, IFamilyProjectRepository, IFamilySubscriptionRepository
Commands/AdCampaigns/, Queries/AdCampaigns/, DTOs/AdCampaigns/, IAdCampaignRepository
Infrastructure/BackgroundJobs/AdBudgetDepletionJob.cs
```

---

## 3. Layer Responsibilities

| Project | Role | Có thể phụ thuộc |
|---|---|---|
| `OrigamiPlatform.API` | Controllers, Middleware, Program.cs, DI wiring | Application (+ Infrastructure chỉ để DI) |
| `OrigamiPlatform.Application` | Commands/Queries + Handlers, DTOs, Validators, Repository interfaces | Domain only |
| `OrigamiPlatform.Domain` | Entities, Enums, Exceptions — zero business logic phức tạp, chỉ enforce invariant cơ bản | ❌ None |
| `OrigamiPlatform.Infrastructure` | EF Core, Repositories, JwtService, EmailService, FileStorageService, Background Jobs | Application + Domain |

---

## 4. Feature Anatomy (Application layer)

```
Application/Commands/[FeatureName]/
├── [Action]Command.cs      ← record, input
└── [Action]Handler.cs      ← class, HandleAsync(), business logic + BR enforcement

Application/Queries/[FeatureName]/
├── [Action]Query.cs
└── [Action]Handler.cs

Application/DTOs/[FeatureName]/
└── [Noun]Dto.cs             ← output, hoặc [Action]Request.cs nếu input phức tạp cần validate riêng

Application/Validators/[FeatureName]/
└── [Action]RequestValidator.cs   ← CHỈ khi validate phức tạp, không bắt buộc mọi Command
```

**Feature → FT mapping:** xem `docs/FT_MAPPING_v5.md` — không lặp lại bảng ở đây để tránh 2 nguồn dễ lệch nhau khi BRD đổi.

---

## 5. Request Flow

```
HTTP Request
  └─► API Controller          (routing, [Authorize], khởi tạo Command/Query, gọi Handler)
        └─► Handler               (business logic, BR validation, ném exception nếu vi phạm)
              └─► Repository Interface  (định nghĩa ở Application/Interfaces/)
                    └─► Infrastructure Repository  (EF Core → SQL Server)
                          └─► Domain Entity
  ◄─── DTO (thành công) hoặc { "error": "..." } (thất bại, qua ExceptionMiddleware)
```

**Quan trọng:** `BlockedWordMiddleware` chặn POST/PUT/PATCH chứa từ cấm **trước khi vào Controller** (BR-23).

---

## 6. Background Jobs

| Job | Lịch chạy | Việc làm |
|---|---|---|
| `SubscriptionExpiryJob` | Hàng ngày, 02:00 UTC | `VipSubscriptions` có `EndDate < UtcNow && Status = Active` → set `Expired` |
| `DailyQuestResetJob` *(chưa code — chỉ cần nếu FT-27 vào scope)* | 00:00 GMT+7 | Reset `UserDailyQuestProgress` |

*Đã xoá `AdBudgetDepletionJob` cùng toàn bộ module AdCampaigns.*

---

## 7. Cross-Feature Communication

**Cho phép:**
- 1 Handler gọi thẳng 1 Handler khác qua constructor DI (ví dụ: `CompleteTutorialStepHandler` có thể cần gọi `UpdateSkillPointsHandler` khi hoàn thành tutorial)
- Domain events / notification trigger qua `INotificationService`

**Không cho phép:**
- Gọi thẳng Repository của feature khác — phải qua Handler
- Reference `Infrastructure` từ `Application` hoặc `API`

---

## 8. Configuration

```
API/appsettings.json
├── ConnectionStrings.Default
├── Jwt.Key / Issuer / Audience / ExpiryMinutes
├── Email.SmtpHost / SmtpPort / From    (Gmail SMTP + App Password)
└── Cloudinary.CloudName / ApiKey / ApiSecret
```
Secrets: `.NET Secret Manager` (dev) hoặc environment variables (prod). Không commit `appsettings.Development.json`.

---

## 9. Tech Stack Summary

| Concern | Choice |
|---|---|
| Language | C# |
| Target framework | .NET 8 — ⚠️ `bin/Debug/` hiện có cả `net8.0` và `net10.0`, cần xác nhận `.csproj` có đang multi-target không chủ đích hay chỉ là leftover build cũ, dọn cho gọn |
| Web framework | ASP.NET Core Web API |
| ORM | Entity Framework Core (Code First) |
| Pattern | Command/Query + Handler (CQRS-lite, không MediatR) |
| Auth | JWT Bearer |
| Email | MailKit (Gmail SMTP) |
| File storage | Cloudinary SDK |
| Password hashing | qua `IPasswordHasher` (BCrypt) |
| Background jobs | `IHostedService` (built-in .NET) |
| Testing | xUnit + Moq |
| API docs | Swagger / Swashbuckle |
