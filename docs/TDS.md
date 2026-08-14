# Technical Design Specification — OriMate Backend (OrigamiPlatform)

| | |
|---|---|
| Project Name | OriMate — Connections Folded Through the Art of Origami |
| Component | Backend — `OrigamiPlatform.*` (.NET 8 Web API, Clean Architecture) |
| SRS Ref | Report 3 — SRS v3.1.0 |
| RTW Ref | Report 3.1 — Requirements Traceability Workbook |
| TDS Version | v1.0.0 |
| Date Created | 07/08/2026 |
| Author(s) | SEP490_G69 |
| Status | Draft |

## Cách đọc file này

File này **không lặp lại nội dung đã có sẵn** trong các doc khác của repo — nó tổng hợp và trỏ tới nguồn thật, đúng tinh thần các doc còn lại trong `docs/`. Khi có mâu thuẫn giữa file này và code, **code luôn đúng** — báo lại để sửa.

Nguồn dùng để biên soạn (đọc trực tiếp code + các doc sau, đối chiếu ngày 07/08/2026):
- `CLAUDE.md`, `docs/BE_ARCHITECTURE.md`, `docs/BE_PROJECT_RULES.md` — kiến trúc & convention
- `docs/Origami_ERD_Design_v3.md` — schema chi tiết
- `docs/PERMISSION_MATRIX.md` — RBAC đối chiếu code thật
- `docs/DEPLOYMENT.md` — hạ tầng Azure
- `docs/FT_MAPPING_v5.md`, `docs/MVP_SCOPE.md` — phạm vi & trạng thái từng FT
- `OrigamiPlatform.API/Program.cs`, `Controllers/*.cs`, `Middleware/ExceptionHandlingMiddleware.cs`, `appsettings.Example.json`, `.github/workflows/build.yml` — đọc trực tiếp mã nguồn

## Document Change History

| Version | Date | Changes | Author |
|---|---|---|---|
| v1.0.0 | 07/08/2026 | Bản khởi tạo — tổng hợp từ code + docs hiện có | Claude (theo yêu cầu TriDV) |

---

# Part 1 — System Architecture

## 1.1 Architecture Overview & Decision Rationale

**Architecture style: Layered Monolith (Clean Architecture), pattern nội bộ "Command/Query + Handler" (CQRS-lite, không dùng MediatR).**

Solution gồm 4 project theo nguyên tắc phụ thuộc một chiều `API → Application → Domain`, `Infrastructure → Application/Domain`. Đây **không phải CQRS đầy đủ** — Command và Query dùng chung một `AppDbContext`, không tách read/write model, không tách database. Gọi đúng tên là **"CQRS-lite"** khi trình bày để tránh hiểu nhầm.

Lý do chọn kiến trúc này:
- Team 3 BE + 2 FE, timeline 3 tuần code — monolith giảm chi phí vận hành/hạ tầng so với microservices, không cần service discovery/message broker.
- CQRS-lite (Command/Handler, không MediatR) giữ boundary rõ ràng giữa write (Command) và read (Query) mà không phải học thêm framework, dễ code nhanh trong thời gian ngắn, dễ review giữa 3 người BE làm song song trên các feature khác nhau.
- Layered dependency rule (`API → Application → Domain`) là "guard rail" chính chống code rối khi 3 người cùng sửa — enforce bằng convention + code review, chưa có ArchUnit test tự động (gap, xem mục 9.2).

| Decision | Choice | Rationale | Constraint |
|---|---|---|---|
| Architecture style | Layered Monolith, Clean Architecture | Team nhỏ (3 BE), timeline 3 tuần | Team size + timeline |
| Pattern nội bộ | Command/Query + Handler, record-based, DI trực tiếp (không MediatR) | Đơn giản, học nhanh, đủ tách biệt logic | Timeline |
| Primary language | C# / .NET 8 | Team đã quen .NET | Team expertise |
| Database | Microsoft SQL Server (Azure SQL khi deploy) | EF Core Code First hỗ trợ tốt, Azure SQL Serverless tiết kiệm credit sinh viên | Azure for Students $100 credit |
| Auth | JWT Bearer (stateless) | Không cần session store dùng chung khi scale nhiều instance sau này | NFR-SEC01 |
| Payment | SePay bank-transfer webhook (tự động) | Không cần tích hợp cổng thanh toán thẻ phức tạp trong 3 tuần, phù hợp thị trường VN | Timeline, chi phí |
| Media storage | Cloudinary | Không tự vận hành file storage, có SDK .NET sẵn | Timeline |

## 1.2 System / Component Diagram

Chưa có file draw.io riêng cho component diagram — sơ đồ runtime hiện mô tả bằng text (khớp `BE_ARCHITECTURE.md` mục 1, đã đối chiếu code thật):

```
Next.js 14 (Frontend, repo riêng orimate-web)
        │ HTTPS REST  /api/...
        ▼
OrigamiPlatform.API  (ASP.NET Core, Controllers + 1 middleware: ExceptionHandlingMiddleware)
        │
        ▼
OrigamiPlatform.Application  (Commands/Queries + Handlers, DTOs, Validators, Repository interfaces)
        │
        ├──► OrigamiPlatform.Domain  (Entities, Enums, Exceptions — zero dependency)
        │
        └──► OrigamiPlatform.Infrastructure
                  ├─ EF Core → SQL Server / Azure SQL
                  ├─ JwtService (JWT issue/validate)
                  ├─ EmailService (MailKit → Gmail SMTP)
                  ├─ FileStorageService (Cloudinary SDK → Cloudinary)
                  ├─ BlockedWordService (in-memory cache)
                  └─ SubscriptionExpiryJob (IHostedService, chạy trong process API)

External inbound: SePay  ──HTTPS webhook, header Authorization: Apikey <key>──►  POST /api/webhooks/sepay
```

| Component | Type | Responsibility | Technology |
|---|---|---|---|
| Next.js Frontend | UI | SPA, gọi REST API | Next.js 14 / React (repo `orimate-web`, ngoài phạm vi TDS này) |
| OrigamiPlatform.API | Service | Routing, auth, exception handling, DI wiring | ASP.NET Core 8, Swashbuckle |
| OrigamiPlatform.Application | Service (in-process) | Business logic (Handler), validation, DTO mapping | C# .NET 8 |
| OrigamiPlatform.Domain | Service (in-process) | Entity, Enum, Exception — không phụ thuộc project nào | C# .NET 8 |
| OrigamiPlatform.Infrastructure | Service (in-process) | EF Core, repository impl, JWT/Email/Cloudinary client, background job | EF Core 8, MailKit, Cloudinary SDK |
| SQL Server / Azure SQL | DB | Lưu toàn bộ dữ liệu hệ thống (metadata, không lưu file nhị phân) | SQL Server 2022 / Azure SQL Serverless |
| Cloudinary | External | Lưu ảnh/video, trả URL HTTPS | Cloudinary SaaS |
| SePay | External Webhook | Gửi sự kiện "tiền vào" đã ký, dùng xác nhận thanh toán VIP tự động | SePay SaaS |
| Gmail SMTP | External | Gửi email verify/reset/notification | SMTP (MailKit client) |

Không có API Gateway, message broker, hay service riêng biệt — tất cả chạy trong 1 process ASP.NET Core duy nhất (đúng bản chất monolith).

## 1.3 Package / Module Diagram

Đã có sẵn, đầy đủ và khớp code thật tại **`docs/BE_ARCHITECTURE.md` mục 2 "Solution Structure"** — không lặp lại ở đây để tránh 2 nguồn lệch nhau. Tóm tắt luồng phụ thuộc:

```
API/Controllers  ──► Application/Commands|Queries (Handler)  ──► Application/Interfaces (I*Repository)
                                                                          ▲
                                                        implement bởi    │
                                                Infrastructure/Repositories
Application/Commands|Queries  ──► Domain/Entities, Domain/Enums, Domain/Exceptions
```

⚠️ Ghi chú trạng thái thực tế (07/08/2026): `Application/Features/Tutorials/` và `Application/Features/AdminConfiguration/` — theo `BE_ARCHITECTURE.md`/`MVP_SCOPE.md` mục 0, các module này đã refactor xong sang `Commands/Tutorials/` và `Commands/AdminConfiguration/` (18/18 FT Must-have Done). Nếu vẫn còn thấy `Features/` trong cây thư mục khi đọc file này, coi là tàn dư chưa dọn — không tạo code mới theo pattern Service ở đó.

## 1.4 Technology Stack

| Layer | Technology | Version | Justification |
|---|---|---|---|
| Language | C# | .NET 8 (LTS) | Team expertise, LTS stability |
| Web Framework | ASP.NET Core Web API | .NET 8 | Built-in DI, middleware pipeline nhẹ |
| ORM | Entity Framework Core | 8.x, Code First | Migration tự động, Fluent API configuration per entity |
| Pattern | Command/Query + Handler (CQRS-lite, không MediatR) | — | Đơn giản, đủ tách biệt cho team nhỏ |
| Primary Database | Microsoft SQL Server | 2022 (local) / Azure SQL Serverless (prod) | ACID, EF Core hỗ trợ tốt nhất |
| Cache | Không dùng — `BlockedWordService` cache in-memory `HashSet` (singleton), không phải cache tầng hạ tầng chung | — | Không cần Redis ở quy mô 1 instance hiện tại |
| Message Queue | Không dùng | — | Không có nhu cầu async job ngoài `IHostedService` nội bộ |
| Frontend | Next.js 14 / React | — | Repo riêng `orimate-web`, ngoài phạm vi backend TDS |
| Authentication | JWT Bearer (access + refresh token), `IPasswordHasher` (BCrypt) | Built-in .NET 8 | Stateless, không cần session store dùng chung |
| Build Tool | `dotnet` CLI / `OrigamiPlatform.slnx` | .NET 8 SDK | — |
| Test Framework | xUnit + Moq | latest | Chuẩn .NET, Arrange/Act/Assert |
| Migration Tool | EF Core Migrations (`dotnet ef`) | Code First | Tích hợp sẵn với ORM |
| Containerisation | Không dùng | — | Deploy thẳng lên Azure App Service (Linux, no container) |
| CI/CD | GitHub Actions (`.github/workflows/build.yml`) | — | Build + test + guard chống merge lại code đã xoá (FamilyProject/Ad) — **chưa có job deploy tự động**, xem mục 9.4 |
| Monitoring | **Chưa triển khai** | — | Không có APM/Prometheus/Grafana — gap, xem Part 8.3 |
| Logging | `ILogger<T>` built-in .NET, ghi ra console/App Service log stream | — | Chưa có structured logging tập trung (ELK/Seq) — gap, xem Part 8.2 |
| File storage | Cloudinary SDK | latest | Ảnh/video, không lưu binary trong DB |
| Email | MailKit (Gmail SMTP, App Password) | latest | Send-only, không cần mail server riêng |
| Payment | SePay webhook (signed, API Key auth) | — | Xác nhận chuyển khoản tự động, thị trường VN |

## 1.5 Deployment Architecture

Chi tiết đầy đủ (từng bước tạo resource, App Settings, đăng ký webhook SePay, quản lý chi phí Azure for Students) đã có ở **`docs/DEPLOYMENT.md`** — không lặp lại ở đây. Tóm tắt kiến trúc production:

```
Internet
   │
   ├── HTTPS ── Next.js frontend (host riêng, ngoài phạm vi BE)
   │
   ├── HTTPS ── Azure App Service "orimate-api" (Linux, .NET 8, tier B1 Basic)
   │                    │
   │                    ├── ADO.NET connection ──► Azure SQL Database "OrimateDb" (tier Serverless, General Purpose)
   │                    ├── HTTPS ──► Cloudinary
   │                    ├── SMTP 587 ──► smtp.gmail.com
   │                    └── (inbound) SePay webhook ──► POST /api/webhooks/sepay
   │
   └── SePay merchant dashboard ──HTTPS signed webhook──► Azure App Service
```

| Aspect | Development | Staging | Production |
|---|---|---|---|
| Infrastructure | Local `dotnet run`, SQL Server local (`Trusted_Connection`) | Chưa có môi trường staging riêng — team 3 tuần dùng thẳng dev → prod | Azure App Service (Linux, B1 Basic) |
| Database | SQL Server local, `OrimateDb_Dev` | — | Azure SQL Database, tier Serverless (auto-pause khi idle) |
| Scaling | Single instance | — | Single instance (chưa cấu hình auto-scale — B1 không hỗ trợ auto-scale) |
| External APIs | SePay sandbox (đăng ký song song lúc dựng hạ tầng), Cloudinary thật, Gmail SMTP thật | — | SePay merchant thật, Cloudinary thật, Gmail SMTP thật |
| Monitoring | Console log | — | App Service log stream (chưa có alerting) |
| Secrets | `.NET Secret Manager` hoặc `appsettings.Development.json` (không commit) | — | Azure App Service → Environment variables (naming `Section__Key` thay cho `Section:Key`) |
| HTTPS redirect | Tắt (`app.UseHttpsRedirection()` chỉ bật khi `!IsDevelopment()` — xem `Program.cs`) | — | Bật |

---

# Part 2 — Interface Specification

## 2.1 Interface Inventory & Classification

Toàn bộ interface là **REST API** — không có SSR page (frontend Next.js là SPA/CSR riêng biệt, gọi API qua HTTP), không có GraphQL, không có event/message queue nội bộ. Có 1 **Webhook Receiver** (SePay) và 1 **Scheduled Job** (`SubscriptionExpiryJob`).

| Controller | Interface Type | FT liên quan | Auth |
|---|---|---|---|
| `AuthController` | REST API | FT-01, FT-02 | Public / `[Authorize]` tuỳ action |
| `AdminController` | REST API | FT-03 | `Roles=Admin` |
| `TutorialsController`, `ReviewController`* | REST API | FT-04–08, FT-11, FT-32 | Mixed — xem Part 2.2/5.2 |
| `LearningController`* (`TutorialProgressController`) | REST API | FT-09, FT-10 | `[Authorize]`, own-resource |
| `CommunityPostsController`, `CommentsController`, `LikesController`, `FollowsController`, `NotificationsController`, `WishlistsController` | REST API | FT-12, FT-13 | `[Authorize]` |
| `ModerationController`, `ReportsController` | REST API | FT-14 | `Roles=ContributorReviewer` (E) / `Roles=Manager,Admin` (F) |
| `UsersController` | REST API | FT-15 | `[Authorize]` / public profile |
| `SubscriptionController` | REST API | FT-16, FT-17 | `[Authorize]`, own-resource |
| `WebhooksController` | **Webhook Receiver** | FT-16 (Giai đoạn 2 — đã live) | `AllowAnonymous` + xác thực bằng header `Authorization: Apikey <key>` |
| `ShopController` | REST API | FT-18 | Public read, `Roles=Admin` write |
| `AchievementsController` | REST API | FT-19, FT-20 | `[Authorize]`, own-resource |
| `JournalsController` | REST API | FT-21 | `[Authorize]`, own-resource |
| `ClanController` | REST API | FT-22 | `[Authorize]` — BE done, **FE chưa có UI** (Inter 3) |
| `WeeklyChallengeController` | REST API | FT-23 (đã code — trạng thái Should-have vượt kế hoạch ban đầu Won't-have, xác nhận qua code thật, không theo `MVP_SCOPE.md` cũ) | `[Authorize]`, `Roles=ContributorReviewer` cho phần chấm |
| `GamificationController` | REST API | FT-25–28, FT-35 | `[Authorize]` |
| `DailyChallengeController` | REST API | FT-34 | `[Authorize]` |
| `LearningPathsController`, `LearningPathModesController` | REST API | FT-33 | Public read, `Roles=Admin,Manager` write |
| `UploadsController` | REST API | Dùng chung cho mọi feature upload ảnh qua Cloudinary | `[Authorize]` |

*Ghi chú: một số tên Controller thật (`ReviewController`, `LearningController`...) khác tên gợi ý trong `FT_MAPPING_v5.md` — danh sách trên lấy trực tiếp từ `ls Controllers/` ngày 07/08/2026, coi là nguồn đúng hơn file mapping cũ.

⚠️ **`FT_MAPPING_v5.md` và `MVP_SCOPE.md` đã cũ so với code thật** tính đến 07/08/2026 — code hiện có thêm hẳn `DailyChallengeController`, `WeeklyChallengeController`, `LearningPathsController`, `LearningPathModesController`, `VisualSearchController`, `UploadsController`, `WebhooksController` (SePay), và `SubscriptionController` đã chuyển hoàn toàn từ xác nhận thủ công sang webhook tự động (khớp `PERMISSION_MATRIX.md`, file mới nhất trong docs). Khi cần danh sách FT ↔ controller chính xác nhất, ưu tiên đọc `PERMISSION_MATRIX.md` (cập nhật 07/08/2026) rồi mới tới `FT_MAPPING_v5.md`.

## 2.2 Authentication & Token Design

**JWT-Based Auth** — xem code thật ở `Program.cs` (JwtBearer config) + `appsettings.Example.json`.

| Aspect | Design Decision |
|---|---|
| Token format | JWT, ký bằng `SymmetricSecurityKey` (HS256 ngầm định của `Microsoft.IdentityModel.Tokens` với `SymmetricSecurityKey`) |
| Access token expiry | 60 phút (`Jwt:AccessTokenExpiryMinutes`, `appsettings.Example.json`) |
| Refresh token expiry | 30 ngày (`Jwt:RefreshTokenExpiryDays`) |
| Refresh token storage (server) | Hash lưu trực tiếp trên `User.RefreshTokenHash` + `User.RefreshTokenExpiresAt` — **không có bảng `RefreshToken` riêng** (đã xác nhận qua `Origami_ERD_Design_v3.md`, khác thiết kế ERD bản đầu) |
| Token storage (client) | Không xác nhận được từ BE — tra FE convention nếu cần (`Authorization: Bearer` header, không phải cookie, theo `API_CONVENTIONS.md`) |
| Token validation | `ValidateIssuer`, `ValidateAudience`, `ValidateLifetime`, `ValidateIssuerSigningKey` đều bật (`Program.cs`) |
| Refresh flow | `POST /api/auth/refresh-token` khi access token hết hạn (401) |
| Logout | `POST /api/auth/logout` — thu hồi refresh token hiện tại |
| Claims | Tối thiểu `NameIdentifier` (dùng ở `User.FindFirstValue(ClaimTypes.NameIdentifier)` trong Controller để lấy `currentUserId`) — chi tiết đầy đủ claims cần đọc `TokenService`/`JwtService` trong `Infrastructure/Services/` |
| Session (KHÔNG áp dụng) | Hệ thống stateless hoàn toàn qua JWT — không dùng session server-side, không cookie session |

**Authorization flow (login):**
```
Client                          API                              DB
  │  POST /api/auth/login          │                                │
  ├────────────────────────────────►                                │
  │                                 │  Verify email + BCrypt hash   │
  │                                 ├───────────────────────────────►
  │                                 │◄───────────────────────────────
  │                                 │  Issue access + refresh JWT   │
  │                                 │  Lưu RefreshTokenHash vào User│
  │                                 ├───────────────────────────────►
  │  { accessToken, refreshToken } │                                │
  │◄────────────────────────────────                                │
```

## 2.3 Server-Side Pages (SSR)

**Không áp dụng** — backend là REST API thuần, không render HTML server-side. Toàn bộ UI thuộc repo Next.js `orimate-web` riêng biệt.

## 2.4 REST API Endpoints

**Nguồn chính xác nhất luôn là Swagger** (`/swagger` khi API chạy dev) — đúng quy ước đã ghi trong `docs/API_CONVENTIONS.md`, không liệt kê lại từng endpoint ở đây để tránh lệch khi code đổi. File `API_CONVENTIONS.md` là **hợp đồng FE/BE chính thức** cho: base URL, format response, status code mapping, naming JSON field, upload file, pagination — TDS này không lặp lại, chỉ tham chiếu.

**Bổ sung 1 endpoint không thuộc "feature FT" nhưng quan trọng về mặt hạ tầng — chưa có trong `API_CONVENTIONS.md`:**

### POST /api/webhooks/sepay — SePay "money in" notification

| Attribute | Value |
|---|---|
| Purpose | Nhận sự kiện chuyển khoản từ SePay, đối chiếu `PaymentCode`, tự động confirm `Transaction` + kích hoạt `VipSubscription` |
| Related SRS Feature | FT-16 (Giai đoạn 2 — đã live, thay thế hoàn toàn xác nhận thủ công của Admin) |
| Authorization | `[AllowAnonymous]` ở tầng ASP.NET, tự xác thực bằng so khớp `Authorization: Apikey <SePay:WebhookApiKey>` bằng `CryptographicOperations.FixedTimeEquals` (chống timing attack) — verify **trước khi** đọc/parse body (đúng nguyên tắc ghi trong code comment, tham chiếu `CLAUDE.md` quy tắc webhook) |

Request:
```
Headers:
  Authorization: Apikey {SePay-WebhookApiKey}
  Content-Type: application/json

Body (SePayWebhookPayloadDto):
{
  "id": "string",            // SePay transaction id — dùng chống xử lý trùng (idempotency key)
  "gateway": "string",
  "transferType": "in" | "out",
  "transferAmount": number,
  "content": "string",       // nội dung chuyển khoản — regex tìm PaymentCode nếu field code trống
  "code": "string|null"      // mã thanh toán do SePay tách riêng (nếu bật tính năng "Mã thanh toán" — prefix OMVIP, xem DEPLOYMENT.md)
}
```

| HTTP Status | Condition | Response Body |
|---|---|---|
| 200 OK | Luôn trả 200 sau khi xác thực chữ ký + parse JSON thành công — **kể cả khi không khớp giao dịch nào (`NoMatch`) hoặc lệch số tiền (`AmountMismatch`)** | `{ "success": true }` |
| 400 Bad Request | Body không parse được JSON | `{ "success": false }` |
| 401 Unauthorized | Header `Authorization` sai/thiếu | (empty) |

Ghi chú thiết kế quan trọng: hệ thống **chủ động không trả lỗi** cho các trường hợp không khớp giao dịch để tránh SePay retry vô ích — mọi kết quả xử lý (`Matched` / `NoMatch` / `AmountMismatch` / `AlreadyProcessed`) đều ghi vào `SePayWebhookLog` (immutable) để audit/debug tranh chấp sau này, không phải để báo lỗi qua HTTP status.

## 2.5 Internal Service Interfaces

Đây chính là **repository interfaces + service interfaces** ở `Application/Interfaces/` (flat, không lồng theo feature) — pattern đã mô tả đầy đủ ở `docs/BE_PROJECT_RULES.md` mục 5.4. Tóm tắt nhóm interface chính:

```csharp
// Repository — 1 interface / entity chính, định nghĩa ở Application, implement ở Infrastructure
ITutorialRepository, IUserRepository, ITransactionRepository, IVipSubscriptionRepository,
ISePayWebhookLogRepository, IAchievementRepository, IJournalRepository, IClanRepository, ...

// Service (I/O ngoài DB) — định nghĩa ở Application, implement ở Infrastructure
IEmailService          // MailKit / Gmail SMTP
ITokenService          // JWT issue/validate + refresh token hash
IPasswordHasher        // BCrypt
IFileStorageService    // Cloudinary upload
IBlockedWordService    // cache in-memory HashSet, kiểm tra nội dung có từ cấm
INotificationService   // tạo Notification record + (tuỳ loại) trigger email
```

Handler gọi thẳng các interface này qua constructor injection — không có tầng "Service tổng hợp" nào khác. Chi tiết method signature từng interface: đọc trực tiếp `Application/Interfaces/*.cs` (danh sách thay đổi theo feature, không đáng để duplicate ở TDS và dễ lệch).

---

# Part 3 — Data Model

## 3.1 Physical Entity Relationship Diagram (ERD)

Chưa có file draw.io — schema đầy đủ (Auth & User, Tutorial Lifecycle, Learning, Community, Monetisation, Achievement/Journal, Clan, Gamification, Shop) đã được audit trực tiếp so với code thật (`ModelSnapshot`, entity `.cs`, `*Configuration.cs`) và viết chi tiết tại **`docs/Origami_ERD_Design_v3.md`** — coi đây là nguồn ERD chính thức, TDS không lặp lại toàn bộ bảng. Tóm tắt số lượng theo module:

| Module | Bảng chính | FT |
|---|---|---|
| Auth & User | `User`, `UserRole`, `UserProfile`, `EmailLog` | FT-01–03 |
| Tutorial Lifecycle | `Category`, `BlockedWord`, `Tutorial`, `TutorialStep`, `TutorialReviewHistory` (immutable), `TutorialVariant` | FT-04–08, FT-11 |
| Learning | `StepProgress`, `StuckThread` | FT-09, FT-10 |
| Community | `CommunityPost`, `CommunityPostMedia`, `Comment`, `Like`, `FollowRelationship`, `Report`, `AuditLog` (immutable), `Notification`, `Wishlist` | FT-12–15 |
| Monetisation | `CreatorVipSettings`, `VipSubscription`, `Transaction`, `SePayWebhookLog` (immutable) | FT-16, FT-17 |
| Achievement & Journal | `Achievement`, `Journal` | FT-19–21 |
| Clan | `Clan`, `ClanMember`, `ClanInvite` | FT-22 |
| Gamification | `DailyQuest`, `UserDailyQuestProgress`, `StreakLog`, `HatGapTransaction` | FT-26–28 |
| Shop | `ShopLink` | FT-18 |

Quy ước chung (khớp `Origami_ERD_Design_v3.md`):
- Mọi PK là `Guid`, `ValueGeneratedNever()` — generate ở Application layer, **ngoại lệ**: `Category` và `BlockedWord` dùng PK `int identity`.
- Mọi bảng có `CreatedAt` (`datetime2`, UTC).
- Mọi string field có `HasMaxLength(n)` tường minh.
- `TutorialReviewHistory`, `AuditLog`, `SePayWebhookLog` là **IMMUTABLE** — chỉ INSERT, không bao giờ UPDATE/DELETE ở tầng Repository.

## 3.2 Database Schema (DDL) — minh hoạ các bảng trung tâm

Toàn bộ field/type/constraint đầy đủ cho **tất cả** bảng nằm ở `Origami_ERD_Design_v3.md` — dưới đây chỉ minh hoạ DDL-style cho các bảng lõi nhất của hệ thống (Tutorial lifecycle + Payment) để làm mẫu convention khi viết migration mới.

```sql
-- TUTORIAL — entity trung tâm. SRS Ref: FT-04..08, DC theo BR-TUT-*
CREATE TABLE Tutorial (
    Id                Guid            PRIMARY KEY,                 -- ValueGeneratedNever
    AuthorId          Guid            NULL REFERENCES [User](Id),  -- NULL nếu IsOfficial = true (BR-PATH-01)
    Title             nvarchar(150)   NOT NULL,                    -- 5-150 ký tự
    Description       nvarchar(500)   NOT NULL,                    -- 20-500 ký tự
    CategoryId        int             NOT NULL REFERENCES Category(Id),
    Difficulty        int             NOT NULL,                    -- enum: Easy/Medium/Hard
    CoverImageUrl     nvarchar(500)   NOT NULL,
    Slug              nvarchar(200)   NOT NULL UNIQUE,              -- giữ nguyên qua các lần Edit (BR-TUT-04)
    ParentTutorialId  Guid            NULL REFERENCES Tutorial(Id), -- self-FK: != NULL => đây là working copy
    Status            int             NOT NULL,                    -- enum TutorialStatus, xem Part 6.1
    Type              int             NOT NULL,                    -- enum: Free/VIP
    IsOfficial        bit             NOT NULL DEFAULT 0,
    CreatedAt         datetime2       NOT NULL,
    UpdatedAt         datetime2       NULL
);

-- TUTORIALREVIEWHISTORY — ⚠️ IMMUTABLE, chỉ INSERT
CREATE TABLE TutorialReviewHistory (
    Id          Guid            PRIMARY KEY,
    TutorialId  Guid            NOT NULL REFERENCES Tutorial(Id),
    ReviewerId  Guid            NOT NULL REFERENCES [User](Id),   -- luôn là Manager/Admin, CTV không tham gia
    Action      int             NOT NULL,                          -- enum: Publish / RejectNeedChanges / Remove
    Reason      nvarchar(500)   NULL,                               -- NULL khi Publish, >=10 ký tự khi Reject/Remove
    CreatedAt   datetime2       NOT NULL
);

-- TRANSACTION — SRS Ref: FT-16 Payment. ReferenceCode nay dùng cho PaymentCode nhập/tách tự động qua SePay
CREATE TABLE [Transaction] (
    Id               Guid            PRIMARY KEY,
    UserId           Guid            NOT NULL REFERENCES [User](Id),   -- Subscriber, người trả tiền
    CreatorId        Guid            NULL REFERENCES [User](Id),       -- Creator nhận subscription
    TransactionType  int             NOT NULL,                          -- enum, hiện chỉ có VipSubscription
    Amount           decimal(18,2)   NOT NULL,
    Status           int             NOT NULL,                          -- PendingConfirmation / Confirmed / Rejected
    ReferenceCode    nvarchar(100)   NULL,                               -- PaymentCode dùng để khớp webhook SePay
    ConfirmedBy      Guid            NULL REFERENCES [User](Id),        -- NULL khi auto-confirm qua webhook
    AdminNote        nvarchar(300)   NULL,
    ConfirmedAt      datetime2       NULL,
    CreatedAt        datetime2       NOT NULL,
    UpdatedAt        datetime2       NULL
);

-- SEPAYWEBHOOKLOG — ⚠️ IMMUTABLE, chống xử lý webhook trùng (idempotency) + audit tranh chấp
CREATE TABLE SePayWebhookLog (
    Id                  Guid            PRIMARY KEY,
    TransactionId       Guid            NULL REFERENCES [Transaction](Id),  -- NULL nếu NoMatch
    SePayTransactionId  nvarchar(100)   NOT NULL,   -- dùng UNIQUE/check tồn tại trước khi xử lý lại
    RawPayload          nvarchar(max)   NOT NULL,   -- lưu nguyên payload để audit
    ProcessedAt         datetime2       NULL,        -- NULL nếu nhận nhưng không match giao dịch nào
    CreatedAt           datetime2       NOT NULL
);
```

## 3.3 ORM / ODM Entity Mapping

EF Core Code First — quy ước bắt buộc (khớp `BE_PROJECT_RULES.md` mục 8):
- Entity class thuần (`Domain/Entities/`), Fluent API configuration riêng từng entity ở `Infrastructure/Persistence/Configurations/`, nạp qua `ApplyConfigurationsFromAssembly` trong `AppDbContext.OnModelCreating` — **không** cấu hình rải rác `entity.Property(...)` trực tiếp trong `OnModelCreating`.
- PK `Guid` dùng `ValueGeneratedNever()` (trừ `Category`/`BlockedWord` — `int identity`).
- Fetch strategy mặc định của EF Core là **Lazy loading KHÔNG bật** (theo convention .NET 8 mặc định) — mọi navigation property cần include tường minh qua `.Include()`/`.ThenInclude()` trong Repository, tránh N+1 khi query danh sách (ví dụ `GetTutorialsQuery` cần `.Include(t => t.Steps)` nếu trả kèm step count).
- `TutorialReviewHistory`, `AuditLog`, `SePayWebhookLog`: **không cấu hình update convention** — Repository chỉ có method `AddAsync`, không có `UpdateAsync`/`DeleteAsync` cho 3 entity này (enforce bằng convention code review, chưa có test tự động chặn — gap).

## 3.4 Indexing & Query Strategy

**Chưa audit index thật trong migration** — cần đọc trực tiếp `Infrastructure/Persistence/Migrations/*.cs` hoặc `ModelSnapshot.cs` để liệt kê index cụ thể đã tạo. Các trường có khả năng cần index theo pattern truy vấn hiện có (dựa trên Query Handler thật, chưa xác nhận đã có index hay chưa):

| Trường | Bảng | Query pattern | Đã có index? |
|---|---|---|---|
| `Slug` | `Tutorial` | `GetTutorialBySlugQuery` — tra cứu theo slug | Có UNIQUE constraint (bắt buộc tạo index) |
| `Status`, `CategoryId` | `Tutorial` | `GetTutorialsQuery` — filter theo trạng thái Published + category | **Chưa xác nhận** — cần kiểm tra migration |
| `Email` | `User` | Login | Có UNIQUE constraint |
| `SePayTransactionId` | `SePayWebhookLog` | Idempotency check mỗi webhook | **Chưa xác nhận** — nên có UNIQUE index, hiện `ExistsBySePayTransactionIdAsync` phụ thuộc query hiệu quả |
| `(UserId, TutorialId)` | `StepProgress`, `Achievement`, `Wishlist` | Unique theo BR | Có UNIQUE constraint theo ERD |

## 3.5 Data Migration Strategy

| Aspect | Decision |
|---|---|
| Migration tool | EF Core Migrations (`dotnet ef migrations add`, `dotnet ef database update`) |
| Migration file location | `OrigamiPlatform.Infrastructure/Persistence/Migrations/` |
| Naming convention | Tên mô tả theo hành động, ví dụ `DropFamilyProjectAndAdTables` (đã áp dụng thật) |
| Rollback strategy | Không có undo script tự động — tạo migration mới để đảo ngược, **không sửa tay migration đã apply** (quy tắc cứng, `BE_PROJECT_RULES.md` mục 8) |
| Run on startup | Không auto-migrate khi start API — chạy `dotnet ef database update` thủ công (local) hoặc CI/CD riêng (Azure) |
| Concurrency khi tạo migration | Chỉ 1 người tạo migration/lần, ping team trước — quy tắc quy trình, không phải cơ chế tự động |
| Seed data | `SeedData.SeedAsync()` chạy tự động khi `app.Environment.IsDevelopment()` trong `Program.cs` — **không chạy ở production**, xem `docs/SEED_DATA.md` để seed thủ công lên Azure SQL trước demo (nhớ đổi password Admin/Manager mặc định) |

---

# Part 4 — Integration & Communication Design

## 4.1 External System Integration Map

| External System | Direction | Protocol | Auth Method | Sync/Async | Error Handling | SRS Ref |
|---|---|---|---|---|---|---|
| SePay | Inbound webhook | HTTPS/REST | API Key trong header `Authorization: Apikey <key>`, so khớp `FixedTimeEquals` | Async (webhook, không blocking user request) | Sai key → 401, không xử lý payload. Không khớp giao dịch/lệch tiền → vẫn 200, ghi log `SePayWebhookLog` để audit thủ công | FT-16 |
| Cloudinary | Outbound | HTTPS/REST (SDK) | API Key + Secret (`Cloudinary:ApiKey/ApiSecret`) | Sync (chờ URL trả về trước khi lưu DB) | Chưa xác nhận có retry — cần đọc `FileStorageService.cs` nếu cần chi tiết | FT-05, FT-14 |
| Gmail SMTP | Outbound | SMTP (587) qua MailKit | App Password (`Email:SmtpAppPassword`) | Sync trong Handler gọi `IEmailService`, không qua queue | Ghi vào `EmailLog` với `Status`/`RetryCount` để debug khi email không tới nơi | Verify/reset/notification email |

## 4.2 Webhook Handling Design (SePay)

| Aspect | Design Decision |
|---|---|
| Signature verification | Không phải HMAC chữ ký payload — SePay dùng **API Key tĩnh** so khớp trong header `Authorization: Apikey <key>`, verify bằng `CryptographicOperations.FixedTimeEquals` (chống timing attack), thực hiện **trước khi** đọc/parse body |
| Idempotency key | `SePayTransactionId` (field `id` trong payload) — check tồn tại qua `ISePayWebhookLogRepository.ExistsBySePayTransactionIdAsync` trước khi xử lý, trả `AlreadyProcessed` nếu trùng |
| Khớp giao dịch | Ưu tiên field `code` (nếu SePay đã tách sẵn theo prefix `OMVIP`, xem `DEPLOYMENT.md`); fallback: regex tìm `OMVIP[0-9A-F]{32}` trong `content` (nội dung chuyển khoản thô) |
| Response strategy | Xử lý đồng bộ ngay trong request — **không** đẩy ra background queue (khối lượng giao dịch nhỏ, chấp nhận được với quy mô hiện tại) |
| Kết quả có thể | `Matched` (khớp mã + đúng số tiền → auto-confirm + activate VIP), `AmountMismatch` (khớp mã nhưng sai số tiền → không xử lý, chỉ log), `NoMatch` (không tìm thấy `PaymentCode` hợp lệ), `AlreadyProcessed` (trùng `SePayTransactionId`) |
| Retry / Dead letter | Không cần — luôn trả `200 OK` sau khi auth+parse hợp lệ nên SePay không tự động retry; các trường hợp không khớp được audit thủ công qua `SePayWebhookLog`, chưa có alert tự động khi phát sinh nhiều `NoMatch`/`AmountMismatch` (gap, xem Part 8.3) |

## 4.3 Synchronous External API Integration

### SePay — VIP Subscription Payment Confirmation

| Aspect | Detail |
|---|---|
| Webhook URL (production) | `https://orimate-api.azurewebsites.net/api/webhooks/sepay` (đăng ký trong SePay merchant dashboard, xem `DEPLOYMENT.md`) |
| Triggered by | User bấm Subscribe VIP → `SubscribeCommand` tạo `Transaction` (`PendingConfirmation`) + `PaymentCode` hiển thị QR chuyển khoản → user chuyển khoản → SePay gọi webhook |
| Timeout | Không áp dụng — hệ thống không chủ động gọi ra SePay, chỉ nhận webhook |
| Retry policy | Không áp dụng chiều outbound |
| Fallback | Chưa có UI xác nhận thủ công dự phòng trong code hiện tại — nếu webhook lỗi/không tới, `Transaction` vẫn ở `PendingConfirmation` (cần xử lý thủ công qua truy vấn DB, không có endpoint Admin confirm tay nào còn hoạt động theo thiết kế Giai đoạn 2 hiện tại — khác với `MVP_SCOPE.md`/`FT_MAPPING_v5.md` cũ vốn mô tả Admin confirm thủ công là Giai đoạn 1; cần xác nhận với team liệu endpoint xác nhận tay có còn giữ làm fallback hay đã gỡ hoàn toàn) |

### Cloudinary — Media Upload

| Aspect | Detail |
|---|---|
| Triggered by | `UploadsController` — FE gửi file lên BE, BE forward lên Cloudinary rồi trả `url` (FE không upload thẳng client → Cloudinary, đúng quy ước `API_CONVENTIONS.md` mục 7) |
| Giới hạn dung lượng | Theo BR từng feature — ví dụ ảnh Achievement ≤10MB |

## 4.4 Asynchronous Communication Design

Không dùng message broker (RabbitMQ/Kafka/SQS) — cơ chế async duy nhất trong hệ thống là **`IHostedService`** built-in .NET, chạy trong cùng process API:

| Job | Schedule | Việc làm |
|---|---|---|
| `SubscriptionExpiryJob` | Hàng ngày, 02:00 UTC | `VipSubscription` có `EndDate < UtcNow && Status = Active` → set `Expired` |

Các job khác được nhắc tới trong `FT_MAPPING_v5.md` (`DailyQuestResetJob`, `ClanQuestResetJob`, `LeagueResetJob`, `ChallengeResultJob`) — **chưa xác nhận đã code hay chưa** tính đến 07/08/2026, dù `DailyChallengeController`/`WeeklyChallengeController` đã tồn tại; cần grep `Infrastructure/BackgroundJobs/` trực tiếp nếu cần biết chính xác trạng thái mới nhất trước khi ước lượng công việc còn lại.

## 4.5 Transaction Boundary Design (Monolith)

| Use Case / Method | Transaction Scope | Operations INSIDE Tx | Operations OUTSIDE Tx |
|---|---|---|---|
| `ProcessSePayWebhookHandler.HandleAsync` | Không dùng `[Transactional]`/explicit transaction tường minh trong code đã đọc — mỗi thay đổi (`UpdateAsync` Transaction, `AddAsync` VipSubscription, `AddAsync` SePayWebhookLog) gọi `SaveChangesAsync` riêng qua Repository. **Rủi ro: nếu request bị ngắt giữa chừng, có thể xảy ra Transaction=Confirmed nhưng VipSubscription chưa tạo** — chưa xác nhận có transaction bao ngoài ở tầng Repository/DbContext hay không, cần review trực tiếp `AppDbContext`/base Repository nếu muốn khẳng định chắc chắn (gap, xem Part 6.2) | — |
| `SubmitTutorialHandler` (theo mẫu chuẩn ở `BE_PROJECT_RULES.md`) | Cập nhật `Tutorial.Status` trong 1 lần `UpdateAsync` | Validate + đổi status | Gửi notification thật (nếu có) nên nằm ngoài — **quy ước ghi trong `CLAUDE.md`/template gốc, chưa xác nhận đã áp dụng nhất quán ở mọi Handler thật** |

⚠️ Đây là gap thật cần review kỹ hơn (không phải suy đoán vô căn cứ — dựa trên việc code `ProcessSePayWebhookHandler` gọi nhiều `Repository.XxxAsync` riêng lẻ không thấy `BeginTransaction`/`IDbContextTransaction` tường minh): nên xác nhận `AppDbContext.SaveChangesAsync` có được gọi 1 lần duy nhất cho cả Handler (qua Unit of Work ẩn trong DI scope) hay mỗi Repository method tự `SaveChangesAsync` riêng — nếu là vế sau, đây là rủi ro consistency thật cho luồng thanh toán, nên ưu tiên xử lý trước khi tăng traffic thật.

---

# Part 5 — Security Design

## 5.1 Authentication Flow
→ Xem Part 2.2.

## 5.2 Authorization & RBAC Implementation

| Aspect | Decision |
|---|---|
| RBAC implementation | `[Authorize(Roles = "...")]` attribute trên từng Controller/action — role-based đơn giản, không dùng policy-based authorization phức tạp |
| Role storage | Bảng `UserRole` (N-N thật với `User`, dù `AssignRoleCommand` hiện chỉ gán 1 role tại một thời điểm — thay thế toàn bộ, không cộng dồn — theo `PERMISSION_MATRIX.md` mục "Thay đổi cấu trúc" #4) |
| Role hierarchy | Không có hierarchy phân cấp tự động (không phải `ADMIN > MANAGER > ...` kiểu kế thừa quyền) — mỗi action khai báo rõ role được phép, ví dụ `Roles="Admin,Manager"` |
| Permission check layer | Role-gate ở Controller (`[Authorize(Roles=...)]`); **ownership check** (chủ sở hữu resource) nằm trong Handler — ví dụ đa số API ghi nội dung cá nhân (tutorial draft, post, comment, wishlist, journal, clan...) chỉ có `[Authorize]` chung, quyền thực tế đến từ so khớp `UserId` trong Handler, không phải role riêng |
| Roles thật trong `UserRoleType` | `User`, `ContributorReviewer`, `Manager`, `Admin` — **không có role `Creator` hay `AdvertisingPartner`** trong enum; Creator là persona của User đã đăng tutorial, không phải giá trị role (khớp `PERMISSION_MATRIX.md`) |
| Failed auth response | 401 khi chưa đăng nhập/token hết hạn; 403 khi đủ đăng nhập nhưng sai role/không phải chủ sở hữu — không tiết lộ thông tin role trong message lỗi |

Ma trận quyền đầy đủ theo từng action (đã đối chiếu trực tiếp với `[Authorize(Roles=...)]` trong toàn bộ Controller + route FE thật): **`docs/PERMISSION_MATRIX.md`** — không lặp lại ở TDS, đây là nguồn RBAC chính thức và mới nhất (07/08/2026), **ưu tiên hơn** `FT_MAPPING_v5.md`/SRS khi có mâu thuẫn về quyền.

## 5.3 Data Protection

| Data Category | At Rest | In Transit | Notes |
|---|---|---|---|
| Toàn bộ traffic | — | HTTPS (bắt buộc ở production qua `UseHttpsRedirection()`, **tắt ở dev**) | Program.cs: `if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();` |
| Password | BCrypt qua `IPasswordHasher` | HTTPS | Never log |
| PII (email, display name) | Không mã hoá riêng ở tầng DB (SQL Server TDE nếu Azure SQL bật mặc định — chưa xác nhận cấu hình) | HTTPS | Chưa có cơ chế xoá/anonymize PII theo yêu cầu (không có DSAR flow) — gap nếu cần compliance |
| Payment | Không lưu thông tin thẻ/tài khoản ngân hàng người dùng — chỉ lưu `Amount`, `ReferenceCode`, trạng thái giao dịch nội bộ. Số tài khoản nhận tiền (`BankAccount:*`) là **của platform**, không phải của user | HTTPS | Delegate xác nhận cho SePay |
| JWT / Refresh token | Refresh token lưu dạng **hash** trên `User.RefreshTokenHash` (không lưu plaintext) | HTTPS, header `Authorization: Bearer` | Access token không lưu ở server (stateless) |
| API keys / secrets | `appsettings.Development.json` (không commit) hoặc Azure App Service Environment variables | — | `appsettings.Example.json` là template public, không chứa giá trị thật |

**Gap đã biết:** chưa cấu hình security header (`Strict-Transport-Security`, `X-Content-Type-Options`, `X-Frame-Options`, CSP) — `Program.cs` hiện chỉ có `UseCors()` + `UseHttpsRedirection()` (prod), không có middleware set header bảo mật bổ sung. Nên thêm nếu ứng dụng cần vượt qua security review nghiêm ngặt hơn.

## 5.4 Input Validation Strategy

| Validation Type | Where Applied | Library / Mechanism | Example |
|---|---|---|---|
| Validate đơn giản | Đầu Handler (tay, `private static void Validate(...)`) | Không dùng library, code tay | `CreateAchievementHandler` |
| Validate phức tạp | `Validators/[Feature]/` | FluentValidation | `LoginRequestValidator`, `ChangePasswordRequestValidator`, validators của `AdminConfiguration` |
| Business rule validation | Trong Handler, ném `DomainException` | — | Ví dụ: `Tutorial.Steps.Count is < 3 or > 30` |
| SQL injection prevention | Repository layer | EF Core parameterised query (LINQ) — không có raw SQL string concatenation trong code đã đọc | — |
| XSS prevention | **Không áp dụng phía server render** (API-only, không render HTML) — trách nhiệm escape thuộc về Next.js frontend khi hiển thị | — | — |
| Blocked-word check (nội dung do user nhập) | ⚠️ **Gọi trực tiếp `IBlockedWordService` bên trong từng Handler ghi nội dung** (`CreateTutorialHandler`, `CreateCommunityPostHandler`, `AddCommentHandler`, `CreateJournalHandler`, `UpdateTutorialHandler`, `SubmitWeeklyChallengeHandler`, `SubmitDailyChallengeHandler`...) — **KHÔNG phải qua `BlockedWordMiddleware` chặn ở tầng HTTP như `CLAUDE.md`/`BE_ARCHITECTURE.md` mô tả**. Đã xác nhận: `Program.cs` chỉ đăng ký duy nhất `ExceptionHandlingMiddleware`, không có `UseMiddleware<BlockedWordMiddleware>()` nào. Đây là **sai lệch thật giữa doc kiến trúc và code** — nên báo lại team để cập nhật `CLAUDE.md`/`BE_ARCHITECTURE.md`, hoặc bổ sung middleware thật nếu ý định ban đầu là chặn tập trung | `BlockedWordService.cs` (Infrastructure) |
| File upload | `UploadsController` | Giới hạn dung lượng/loại file theo BR từng feature (chưa audit whitelist MIME type cụ thể) | — |

## 5.5 Security Headers & CORS Policy

CORS thật (`Program.cs`):
```
policy.WithOrigins("http://localhost:3000", "https://localhost:3000")
      .AllowAnyHeader()
      .AllowAnyMethod();
```

| Aspect | Trạng thái thật |
|---|---|
| CORS | Chỉ whitelist `localhost:3000` (dev) — **chưa thấy origin production trong `Program.cs`**; `appsettings.Example.json` có key `AllowedOrigins` nhưng `Program.cs` đọc origin bằng literal string, chưa xác nhận có đọc từ config hay không — cần kiểm tra kỹ trước khi deploy domain production thật, nếu không FE prod sẽ bị CORS chặn |
| `AllowCredentials` | Không bật (`AllowAnyHeader().AllowAnyMethod()` không có `.AllowCredentials()`) — hợp lý vì dùng Bearer token, không dùng cookie |
| Security headers (HSTS, X-Frame-Options, CSP, X-Content-Type-Options) | **Chưa cấu hình** — gap, xem Part 5.3 |

---

# Part 6 — Key Algorithms & Business Logic

## 6.1 State Machine — Tutorial

Pattern: **Plain conditionals trong Handler** (không dùng State Pattern hay Spring/Stateless-machine library) — đủ đơn giản cho số lượng trạng thái hiện tại và team size.

Enum thật (`Domain/Enums/TutorialStatus.cs`):
```csharp
public enum TutorialStatus
{
    Draft,
    PendingManagerReview,
    RevisionRequired,
    Published,
    Removed,
    EditPendingReview,
    Merged   // BR-17: terminal, chỉ dùng cho working-copy row sau khi Manager approve edit — không dùng cho Tutorial gốc
}
```

Transition chính (tổng hợp từ `PERMISSION_MATRIX.md` mục 2 + comment code):

| From | Event | To | Guard / Actor |
|---|---|---|---|
| `Draft` | Submit | `PendingManagerReview` | Creator, đủ 3-30 bước |
| `PendingManagerReview` | Publish | `Published` | Manager/Admin |
| `PendingManagerReview` | Reject cần sửa | `RevisionRequired` | Manager/Admin, lý do ≥10 ký tự |
| `PendingManagerReview` | Remove | `Removed` | Manager/Admin — terminal |
| `RevisionRequired` | Sửa & nộp lại | `PendingManagerReview` | Creator — **không terminal**, lặp lại nhiều lần được |
| `Published` | Tạo working copy & sửa | (working copy mới, `Status=EditPendingReview`, `ParentTutorialId` trỏ về bản gốc) | Creator — bản gốc vẫn `Published` song song |
| `EditPendingReview` (working copy) | Manager approve merge | `Merged` (working copy) + cập nhật nội dung vào Tutorial gốc | Manager/Admin |
| `Published` | Remove | `Removed` | Manager/Admin — terminal |
| bất kỳ (Admin/Manager tự tạo) | `POST /admin` publish trực tiếp | `Published` (gắn `IsOfficial`) | Chỉ Admin/Manager, bỏ qua hàng đợi review |

Invalid transition response: `DomainException` → HTTP 400 (qua `ExceptionHandlingMiddleware`).

**Chỉ 1 vòng duyệt duy nhất do Manager** — không có review 2 vòng (CTV không tham gia duyệt tutorial, chỉ xử lý Weekly/Daily Challenge và xoá comment vi phạm trực tiếp).

## 6.2 State Machine — Transaction / VipSubscription (Payment)

```
Transaction.Status:  PendingConfirmation ──(SePay webhook, Matched)──► Confirmed
                                          └──(không khớp/không xử lý)──► giữ nguyên PendingConfirmation (không có state Rejected tự động thấy trong ProcessSePayWebhookHandler)

VipSubscription.Status: (tạo mới)Active ──(SubscriptionExpiryJob, EndDate < UtcNow)──► Expired
```

Không có transition `Cancelled` chủ động (user tự huỷ) thấy trong `ProcessSePayWebhookHandler` — nếu FE có nút "Huỷ subscription", cần audit `SubscriptionController` riêng để xác nhận có Command tương ứng hay chưa.

## 6.3 Concurrency & Consistency Handling

| Operation | Race Condition Risk | Strategy thật (đã xác nhận) |
|---|---|---|
| SePay webhook trùng | 2 lần gọi webhook cùng `SePayTransactionId` (SePay tự retry hoặc gọi nhầm) | Check `ExistsBySePayTransactionIdAsync` trước khi xử lý — **không phải lock DB, là check-then-act**, về lý thuyết vẫn có race nhỏ nếu 2 request đến đồng thời trước khi log được ghi (chưa xác nhận có unique constraint DB-level trên `SePayTransactionId` để chặn tuyệt đối — nên có, xem Part 3.4) |
| Ghi nội dung đồng thời (post/comment/tutorial) | Không có `@Version`/optimistic lock nào được xác nhận trong entity đã đọc | **Chưa xác nhận cơ chế optimistic/pessimistic locking** — rủi ro chấp nhận được ở quy mô hiện tại (concurrent write cùng 1 row hiếm), nhưng là gap nếu traffic tăng |
| Transaction boundary trong Handler thanh toán | Nhiều `SaveChangesAsync` rời rạc trong 1 Handler | Xem Part 4.5 — **gap cần review**, chưa xác nhận có Unit-of-Work bao ngoài |

## 6.4 Transaction Boundary Design
→ Xem Part 4.5 (đã gộp, tránh trùng nội dung giữa Part 4 và Part 6 như 2 template gốc yêu cầu riêng).

## 6.5 Scheduled Jobs & Background Tasks

| Job | Schedule (cron-like) | Description | Idempotency | Failure Handling |
|---|---|---|---|---|
| `SubscriptionExpiryJob` | Hàng ngày 02:00 UTC | `VipSubscription.Status: Active → Expired` khi `EndDate < UtcNow` | Có — chỉ cập nhật record thoả điều kiện, chạy lại nhiều lần không gây sai lệch | Chưa xác nhận có alert khi job fail — chạy trong process `IHostedService`, lỗi sẽ log qua `ILogger` nếu có try/catch nội bộ (cần đọc trực tiếp file job nếu cần chắc chắn) |

Các job khác (`DailyQuestResetJob`, `ClanQuestResetJob`, `LeagueResetJob`, `ChallengeResultJob`) — trạng thái code thật **chưa xác nhận** tại thời điểm viết TDS này, xem ghi chú Part 4.4.

## 6.6 Notification Dispatch Design

Đơn giản hoá so với template gốc — **không có multi-channel routing** (Facebook/Zalo/Shopee messaging) như SRS Part 0 có nhắc "kênh nguồn" cho tutorial, nhưng notification dispatch thật hiện chỉ có 2 kênh:
1. **In-app**: ghi record vào bảng `Notification` (`IsRead=false`), FE poll/hiển thị.
2. **Email**: qua `IEmailService` cho sự kiện quan trọng (verify, reset password, có thể cả review result — cần xác nhận danh sách trigger chính xác ở `INotificationService` implementation nếu cần đầy đủ).

Không có retry queue riêng cho notification — nếu `IEmailService` lỗi, ghi vào `EmailLog.Status=Failed` để debug thủ công, không có cơ chế tự động retry theo lịch (khác với pseudocode retry 3 lần trong template gốc — **chưa xác nhận đã implement**, cần đọc `EmailService.cs`/`EmailLog` handler nếu cần khẳng định).

---

# Part 7 — Performance & Scalability Design

⚠️ Phần này **chưa có target/số liệu chính thức** trong SRS/RTW đã đọc, và **chưa có triển khai thật** (không có cache tầng hạ tầng, không có load test, không có auto-scaling) — dự án là MVP 3 tuần cho đồ án tốt nghiệp, không phải hệ thống production quy mô lớn. Ghi rõ trạng thái thay vì bịa số liệu.

## 7.1 Caching Strategy

| What is Cached | Cơ chế | Ghi chú |
|---|---|---|
| Blocked word list | In-memory `HashSet` trong `BlockedWordService` (đăng ký `Singleton`) | Không phải cache phân tán (Redis) — chỉ hợp lý khi 1 instance API. Nếu scale nhiều instance sau này, cần đổi sang cache dùng chung hoặc load lại theo TTL |
| Mọi thứ khác | Không cache | Query thẳng SQL Server mỗi lần |

## 7.2 Database Query Optimization

Chưa có audit thật về N+1 hay volume estimate cho từng query — khuyến nghị review `Include()`/`ThenInclude()` trong các Repository trả danh sách lớn (`GetTutorialsQuery`, `GetCommunityFeed`) trước khi coi là production-ready.

Pagination: theo `API_CONVENTIONS.md` mục 8 — `page` mặc định 1, `pageSize` mặc định 20, tối đa 100 (BE tự cap).

## 7.3 Scaling Approach

| Hiện trạng | Ghi chú |
|---|---|
| Single instance | Azure App Service tier B1 Basic — không auto-scale (tier này không hỗ trợ) |
| Stateless design | JWT không cần session store dùng chung → về lý thuyết sẵn sàng horizontal scale nếu nâng tier, nhưng `BlockedWordService` cache in-memory (Singleton) cần đổi cơ chế nếu chạy nhiều instance (xem 7.1) |
| DB | Azure SQL Serverless — tự scale compute trong giới hạn tier, không phải scale ứng dụng |

## 7.4 Load Testing Plan

**Chưa thực hiện.** Không có k6/JMeter script hay kết quả load test nào trong repo tại thời điểm viết TDS này.

---

# Part 8 — Error Handling & Observability

## 8.1 Error Code Registry

Hệ thống thật **không dùng error code dạng chuỗi** (`AUTH_001`, `ORDER_001`...) như template gốc — response lỗi chỉ có 1 field `error` (message người đọc được), theo đúng `API_CONVENTIONS.md`. Mapping HTTP status thật (`ExceptionHandlingMiddleware.cs`, đã đọc trực tiếp):

| Exception | HTTP Status | Response Body |
|---|---|---|
| `ConflictException` | 409 | `{ "error": "<message>" }` |
| `NotFoundException` | 404 | `{ "error": "<message>" }` |
| `ForbiddenException` | 403 | `{ "error": "<message>" }` |
| `DomainException` | 400 | `{ "error": "<message>" }` |
| Bất kỳ exception khác (unhandled) | 500 | `{ "error": "An unexpected error occurred." }` — message thật ghi vào log qua `_logger.LogError(ex, ...)`, **không** trả raw exception cho client |

Ghi chú: `ConflictException` (409) là 1 exception type có thật trong `Domain/Exceptions/` **không được nhắc tới** trong `CLAUDE.md`/`BE_PROJECT_RULES.md` (2 file đó chỉ liệt kê `DomainException`/`NotFoundException`/`ForbiddenException`) — nên bổ sung vào 2 doc đó để đồng bộ, dùng cho case như "webhook trùng"/"resource đã tồn tại" theo đúng ngữ nghĩa HTTP 409.

## 8.2 Logging Standard

| Field | Trạng thái thật |
|---|---|
| Cơ chế | `ILogger<T>` built-in .NET, output ra console (dev) / App Service log stream (prod) |
| Correlation ID | **Chưa triển khai** — không có header `X-Correlation-ID` hay middleware gán trace ID xuyên suốt request. Gap nếu cần debug đa dịch vụ (hiện là monolith nên ít cấp thiết hơn, nhưng vẫn hữu ích để nối log 1 request qua nhiều Handler) |
| Structured logging | Chưa xác nhận dùng structured log (Serilog/Seq) hay chỉ log text thô qua `ILogger` mặc định |
| Level dùng thật | `LogError` xác nhận có ở `ExceptionHandlingMiddleware` cho lỗi 500; các level khác (`Warn`/`Info`/`Debug`) tuỳ Handler, chưa audit toàn bộ |
| Never log | Password, JWT, App Password SMTP/Cloudinary secret — chưa audit tự động (không có scrub filter), dựa vào convention code review |

## 8.3 Monitoring & Alerting

**Chưa triển khai** — không có APM, không có alert threshold, không có dashboard. Theo dõi hiện tại hoàn toàn thủ công qua Azure Portal → App Service → Log stream / Metrics cơ bản có sẵn của Azure (CPU, memory, HTTP response time mức platform), không có ngưỡng cảnh báo tự động cấu hình riêng.

Rủi ro cụ thể nên theo dõi thủ công trước demo: `SePayWebhookLog` có nhiều record `NoMatch`/`AmountMismatch` bất thường (dấu hiệu lỗi khớp giao dịch), `SubscriptionExpiryJob` có chạy đúng lịch hay không (không có alert nếu job fail âm thầm).

## 8.4 Distributed Tracing

**Không áp dụng** — monolith 1 process, không có tracing tool (Zipkin/Jaeger/OTel) nào được cấu hình.

---

# Part 9 — Development Guidelines

## 9.1 Project & Package Structure

Đã có đầy đủ, chính xác tại **`docs/BE_ARCHITECTURE.md` mục 2** — không lặp lại.

## 9.2 Coding Standards & Conventions

Đã có đầy đủ tại **`docs/BE_PROJECT_RULES.md`** (naming convention, code pattern mẫu, anti-pattern, DI setup, EF Core rules, Git workflow) — đây là nguồn chuẩn coding style cho toàn team, TDS không lặp lại. Điểm nhấn cho người mới đọc TDS trước khi code:
- Business logic sống trong **Handler**, không trong Controller.
- Không return `null` để báo lỗi — luôn `throw` exception cụ thể (`DomainException`/`NotFoundException`/`ForbiddenException`/`ConflictException`).
- Không tạo Service pattern mới kiểu cũ (`XxxService`) — dùng Command/Handler.
- Chưa có ArchUnit-style test tự động chặn vi phạm dependency direction (`API → Application → Domain`) — hiện enforce bằng code review + convention, là gap nếu team lớn hơn.

## 9.3 Testing Strategy

| Test Type | Scope | Tool | Trạng thái thật |
|---|---|---|---|
| Unit test | Handler (Command/Query) | xUnit + Moq, pattern Arrange/Act/Assert | Có — cấu trúc `OrigamiPlatform.Tests/Commands|Queries|Controllers/`, chạy trong CI (`build.yml` bước "Run tests") |
| Integration test | Chưa audit chi tiết coverage | `OrigamiPlatform.IntegrationTests` (project riêng tồn tại trong solution) | Có project, chưa xác nhận coverage thật |
| State transition test | Tutorial status | xUnit | Chưa xác nhận coverage 100% các nhánh |
| API contract test | — | Không dùng (không có Pact/Spring Cloud Contract tương đương) | Swagger là nguồn contract, không test tự động khớp |
| Load test | — | — | **Chưa có**, xem Part 7.4 |
| Security test | — | — | **Chưa có** (không có OWASP ZAP scan tự động trong CI) |

Coverage target: không ép cứng 80% — ưu tiên test đúng các Business Rule quan trọng (VIP gating, tutorial review, blocked word), theo `BE_PROJECT_RULES.md` mục 10.

## 9.4 CI/CD Pipeline Design

Pipeline thật (`.github/workflows/build.yml`, đã đọc trực tiếp):

```
[Push/PR vào branch master]
      │
      ▼
1. Checkout code
      │
      ▼
2. Setup .NET (9.0.x — LƯU Ý: khác .NET 8 dùng để build app, cần xác nhận có ý định multi-target hay là cấu hình CI leftover chưa cập nhật)
      │
      ▼
3. dotnet restore
      │
      ▼
4. dotnet build --configuration Release (không fail trên warning, chỉ cảnh báo)
      │
      ▼
5. dotnet test --configuration Release
      │
      ▼
6. Guard chống merge nhầm code FamilyProject/AdCampaign đã xoá — grep toàn bộ *.cs, fail CI nếu tìm thấy (trừ Migrations/)
```

| Aspect | Decision |
|---|---|
| CI tool | GitHub Actions |
| Branch trigger | `push`/`pull_request` vào `master` — **lưu ý**: cần xác nhận đây có đúng là default branch hiện tại của repo hay không trước khi coi pipeline là "đang chạy trên mọi merge" |
| Deploy tự động | **Chưa có** — `DEPLOYMENT.md` chỉ đề xuất job `deploy` mẫu (`azure/webapps-deploy@v3`), chưa xác nhận đã thêm thật vào `build.yml` hay file `deploy.yml` riêng. Deploy hiện tại (nếu có) là thủ công theo hướng dẫn `DEPLOYMENT.md` |
| PR requirement | Tối thiểu 1 reviewer, build phải pass — quy tắc quy trình (`BE_PROJECT_RULES.md` mục 9), CI hiện chưa cấu hình branch protection rule bắt buộc (cần kiểm tra GitHub repo settings riêng, ngoài phạm vi đọc code) |
| Rollback | Không có cơ chế tự động — redeploy thủ công theo `DEPLOYMENT.md` nếu cần |

---

# References & Open Items

## Referenced Documents

| Document | Location | Purpose |
|---|---|---|
| SRS v3.1.0 (Report 3) | (ngoài repo BE — do FE/PM giữ, nội dung được người dùng dán vào yêu cầu tạo TDS này) | Requirements baseline |
| `CLAUDE.md` | root repo | Convention cho AI coding agent |
| `docs/BE_ARCHITECTURE.md` | `docs/` | Kiến trúc solution chi tiết |
| `docs/BE_PROJECT_RULES.md` | `docs/` | Coding rules, naming, pattern mẫu |
| `docs/API_CONVENTIONS.md` | `docs/` | Hợp đồng FE/BE — format response, status code |
| `docs/Origami_ERD_Design_v3.md` | `docs/` | Schema DB đầy đủ |
| `docs/PERMISSION_MATRIX.md` | `docs/` | RBAC đối chiếu code thật — nguồn quyền chính xác nhất |
| `docs/DEPLOYMENT.md` | `docs/` | Hạ tầng Azure, đăng ký SePay |
| `docs/FT_MAPPING_v5.md` | `docs/` | Mapping FT ↔ feature (⚠️ đã cũ so với code, xem Part 2.1) |
| `docs/MVP_SCOPE.md` | `docs/` | Phạm vi 3 tuần (⚠️ đã cũ về payment flow, xem Part 4.3) |
| `docs/SEED_DATA.md` | `docs/` | Seed data hướng dẫn |
| `docs/FT_FE_SC_AsImplemented.md` | `docs/` | Đối chiếu FT ↔ FE thật |

## Open Items — cần xác nhận với team trước khi coi TDS này là "đã chốt"

1. **Transaction boundary trong `ProcessSePayWebhookHandler`** (Part 4.5, 6.3) — có Unit-of-Work bao ngoài hay mỗi Repository tự `SaveChangesAsync`? Rủi ro consistency thật cho luồng thanh toán nếu là vế sau.
2. **`BlockedWordMiddleware`** được mô tả trong `CLAUDE.md`/`BE_ARCHITECTURE.md` nhưng **không thấy đăng ký trong `Program.cs`** — thực tế check nằm trong từng Handler. Cần quyết định: cập nhật lại 2 doc đó, hay thêm middleware thật để tập trung hoá.
3. **CORS production origin** — `Program.cs` hard-code `localhost:3000`, chưa thấy đọc `AllowedOrigins` từ config hay thêm domain production. Cần xử lý trước khi deploy domain thật.
4. **Fallback xác nhận thanh toán thủ công** — `MVP_SCOPE.md`/`FT_MAPPING_v5.md` mô tả Giai đoạn 1 (Admin confirm tay) nhưng `PERMISSION_MATRIX.md` (mới hơn) nói xác nhận nay hoàn toàn tự động qua webhook, không còn thao tác thủ công. Cần xác nhận có còn giữ endpoint Admin confirm tay làm fallback khi webhook lỗi hay không.
5. **CI target .NET 9.0.x** trong khi ứng dụng khai báo .NET 8 — leftover cấu hình hay chủ đích multi-target? (Cũng được `BE_ARCHITECTURE.md` mục 9 nhắc tới như một note chưa giải quyết.)
6. **Index thật trên các cột truy vấn nhiều** (`Tutorial.Status`, `SePayWebhookLog.SePayTransactionId`) — chưa audit trực tiếp migration để xác nhận đã tạo hay chưa.
7. **Danh sách background job còn thiếu** (`DailyQuestResetJob`, `ClanQuestResetJob`, `LeagueResetJob`, `ChallengeResultJob`) — cần grep `Infrastructure/BackgroundJobs/` để cập nhật trạng thái mới nhất, vì `DailyChallengeController`/`WeeklyChallengeController` đã tồn tại nhưng job hỗ trợ có thể chưa.
