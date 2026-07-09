# CLAUDE.md — OriMate (Origami Community Platform)

## ⚠️ Đọc trước khi code bất kỳ FT nào
1. `docs/MVP_SCOPE.md` — **chỉ code FT nằm ở mục MUST-HAVE / SHOULD-HAVE**. Không tự ý implement FT ở mục WON'T-HAVE (Future Work) dù có thể tìm thấy mô tả đầy đủ trong BRD.
2. `docs/FT_MAPPING_v5.md` — tra FT ↔ feature folder ↔ entity ↔ actor trước khi tạo file mới.
3. File này (`CLAUDE.md`) chỉ chứa **pattern code & convention**, không lặp lại danh sách FT — tránh lệch nguồn khi BRD thay đổi.

## Project overview
Clean Architecture .NET 8 backend cho nền tảng học gấp giấy origami + cộng đồng (OriMate).
Pattern: **CQRS Command/Query + Handler** (record-based) — KHÔNG dùng MediatR, Handler được inject trực tiếp qua DI. Đây là pattern đã dùng thật trong code hiện có (xác nhận qua `Application/Commands/Achievements/*`).
Frontend: Next.js 14 (repo riêng, không nằm trong solution này).

## Solution projects
- **OrigamiPlatform.Domain** — entities, enums, constants. Zero external dependencies, zero business logic.
- **OrigamiPlatform.Application** — `Commands/`, `Queries/`, Handlers, `DTOs/`, `Validators/` (FluentValidation), `Interfaces/` (repository interfaces). Business logic sống trong Handler.
- **OrigamiPlatform.Infrastructure** — EF Core, repository implementations, JwtService, EmailService, FileStorageService (Cloudinary), background jobs.
- **OrigamiPlatform.API** — controllers, middleware, DI wiring, Program.cs.

## Dependency rule (NEVER violate)
```
API → Application → Domain
Infrastructure → Application + Domain
API must NOT reference Infrastructure or Domain directly.
Application must NOT reference Infrastructure (dùng interface).
```

## Architecture rules
- Business logic sống trong Handler — never trong Controller.
- Controller thin: nhận Command/Query → gọi Handler tương ứng → trả kết quả. Không if/else nghiệp vụ.
- Mọi content write (post, comment, journal, tutorial description...) phải qua `IBlockedWordService` **trước** khi lưu — enforce ở `BlockedWordMiddleware`, chặn trước khi vào Controller.
- `TutorialReviewHistories` là IMMUTABLE — chỉ INSERT, không bao giờ UPDATE/DELETE.
- Quy trình duyệt tutorial là **1 vòng duy nhất do Manager** (Publish / Reject-NeedChanges / Remove). CTV **không** tham gia duyệt tutorial — CTV chỉ xử lý Weekly Challenge (chấm Relevance, flag, xoá comment vi phạm rõ ràng).
- Handler ném `DomainException`, `NotFoundException`, hoặc `ForbiddenException` — không bao giờ return null để báo lỗi.

## Feature structure
```
Application/
├── Commands/[FeatureName]/
│   ├── [Action]Command.cs      (record, input)
│   └── [Action]Handler.cs      (class, HandleAsync — chứa business logic)
├── Queries/[FeatureName]/
│   ├── [Action]Query.cs
│   └── [Action]Handler.cs
├── DTOs/
│   └── [Feature]Dto.cs         (output model)
├── Validators/
│   └── [Action]Validator.cs    (FluentValidation, validate Command/Query)
└── Interfaces/
    └── I[Entity]Repository.cs
```
Controller tương ứng nằm ở `API/Controllers/[FeatureName]Controller.cs`, inject thẳng các Handler cần dùng (không qua Service trung gian). Tra bảng feature folder ↔ FT ở `docs/FT_MAPPING_v5.md`.

⚠️ **Ngoại lệ đang refactor (xem `docs/MVP_SCOPE.md` mục 1):** `Features/Tutorials/` và `Features/AdminConfiguration/` hiện vẫn còn ở Service pattern cũ (`TutorialService`, `AdminConfigService`), đang được refactor sang Command/Handler. Trong lúc refactor chưa xong, KHÔNG tạo thêm code mới theo Service pattern ở 2 feature này — chỉ chuyển dần sang `Commands/Tutorials/` và `Commands/AdminConfiguration/`. Sau khi refactor xong, folder `Features/` sẽ được xoá hoàn toàn.

## Code pattern mẫu

```csharp
// Command — record, input
public record SubmitTutorialCommand(Guid TutorialId, Guid AuthorId);

// Handler — constructor injection, không cần base class
public class SubmitTutorialHandler
{
    private readonly ITutorialRepository _repo;
    private readonly INotificationService _notifications;

    public SubmitTutorialHandler(ITutorialRepository repo, INotificationService notifications)
        => (_repo, _notifications) = (repo, notifications);

    public async Task<TutorialDto> HandleAsync(SubmitTutorialCommand cmd, CancellationToken ct = default)
    {
        // BR validate bằng FluentValidation Validator trước khi vào Handler (hoặc validate đầu Handler)
        // implementation ở đây — ném DomainException nếu vi phạm BR
    }
}

// Controller — inject thẳng Handler, không qua Service trung gian
public class TutorialController : ControllerBase
{
    private readonly SubmitTutorialHandler _submitHandler;
    private readonly GetTutorialsHandler _getHandler;

    public TutorialController(SubmitTutorialHandler submitHandler, GetTutorialsHandler getHandler)
        => (_submitHandler, _getHandler) = (submitHandler, getHandler);
}
```

## API response format
```json
// Success: trả DTO trực tiếp
{ "id": "...", "title": "..." }

// Error: luôn theo dạng này (xử lý ở ExceptionMiddleware)
{ "error": "Human-readable message" }
```
HTTP status mapping: `DomainException` → 400, `NotFoundException` → 404, `ForbiddenException` → 403, unhandled → 500.

✅ Đã xác nhận qua `CreateAchievementHandler.cs` thật — Handler trả DTO trực tiếp (`created.ToDto()`), ném `DomainException`/`NotFoundException` từ `Domain.Exceptions`. Validate đơn giản có thể làm tay trong Handler (như `Validate()` private method) — không bắt buộc phải tách FluentValidation Validator riêng cho mọi Command, chỉ tách khi logic validate phức tạp (xem `Auth`, `AdminConfiguration` làm mẫu).

## EF Core conventions
- Fluent API (`IEntityTypeConfiguration<T>`) cho mọi entity — không dùng data annotation.
- Mọi string property phải có `HasMaxLength(n)` — không để `nvarchar(max)` ngoài ý muốn.
- PK là `Guid`, `ValueGeneratedNever()` — generate ở application layer, không phải DB identity.
- Migration nằm ở `Infrastructure/Persistence/Migrations/` — chỉ 1 người tạo migration/lần, không sửa migration đã apply.
- `CreatedAt`/`UpdatedAt` set ở application layer (không dùng DB default).

## Naming conventions
| Item | Convention | Ví dụ |
|---|---|---|
| Feature folder | PascalCase | `Tutorials`, `Clan`, `Gamification` |
| Controller | `[Feature]Controller.cs` | `TutorialController.cs` |
| Command | `VerbNounCommand` | `SubmitTutorialCommand`, `CreateAchievementCommand` |
| Query | `GetNounQuery` | `GetTutorialsQuery` |
| Handler | `VerbNounHandler` / `GetNounHandler` | `SubmitTutorialHandler`, `CreateAchievementHandler` |
| Repository interface/class | `I[Entity]Repository.cs` / `[Entity]Repository.cs` | `ITutorialRepository.cs` |
| DTO output | `NounDto` | `TutorialDto` |
| Validator | `[Action]Validator.cs` | `SubmitTutorialCommandValidator.cs` |
| Không viết tắt | — | `CreateVipSubscriptionCommand`, không viết `CreateVipSubCommand` |

## Key business rules đang trong scope (MUST + SHOULD — xem docs/MVP_SCOPE.md)
| Rule | Constraint |
|---|---|
| BR-AUTH-01 | Email unique, sai credentials trả lỗi generic |
| BR-AUTH-02 | Verify link 24h / reset 1h, single-use; đổi mật khẩu → xoá Refresh Token (logout mọi thiết bị) |
| BR-TUT-01 | Reject không phải trạng thái terminal — chỉ Remove là terminal |
| BR-TUT-02 | Đổi `isVip` hoặc giảm số bước sau publish → cảnh báo Manager |
| BR-TUT-03 | Không đổi `isVip` khi đang có progress active |
| BR-TUT-04 | Progress vượt quá số bước mới (sau edit) → chuyển archived |
| BR-VIP-01 | Step 1-3 free, step 4+ khoá **server-side** (không chỉ ẩn UI) |
| BR-VIP-02 | 30 ngày cố định, không auto-renew, không hoàn tiền |
| BR-VIP-03 | Creator phải có `CreatorVipSettings` active trước khi đánh dấu `isVip` |
| BR-VIP-04 | Hạt Gấp KHÔNG bao giờ mở VIP tutorial — VIP chỉ mở qua subscription |
| BR-PAYMENT-01 | **Thanh toán qua SePay** — webhook `POST /api/v1/webhooks/sepay`, bắt buộc verify `X-SePay-Signature` trước khi xử lý, bắt buộc idempotency check (không cộng tiền/kích hoạt VIP 2 lần cho cùng 1 giao dịch) |
| BR-CLAN-01 | 1 user chỉ ở 1 Clan tại 1 thời điểm |
| BR-CLAN-03 | Owner không rời Clan trực tiếp — phải chuyển Owner trước |
| BR-QUEST-01 | Daily Quest reset 00:00 GMT+7 |
| BR-SEEDS-01 | Hạt Gấp không mua bằng tiền thật, không gacha |
| BR-PORTFOLIO-01 | Achievement mặc định Private, user tự bật Public |
| BR-23 | Mọi content text phải pass blocked word check trước khi lưu |

*(Danh sách đầy đủ BR — kể cả phần Won't-have — xem BRD v5.0, không copy hết vào đây để tránh AI code nhầm phạm vi.)*

## Background jobs
`IHostedService`, đăng ký trong `Infrastructure/DependencyInjection.cs`:
| Job | Lịch chạy | Việc làm |
|---|---|---|
| `SubscriptionExpiryJob` | Hàng ngày 02:00 UTC (~09:00 GMT+7) | `VipSubscriptions` có `EndDate < UtcNow && Status = Active` → set `Expired` |
| `DailyQuestResetJob` | Hàng ngày 00:00 GMT+7 | Reset `UserDailyQuestProgress` — *chỉ cần nếu FT-27 nằm trong scope tuần đó, xem MVP_SCOPE.md* |

*Đã bỏ `AdBudgetDepletionJob` — hệ quảng cáo không còn trong scope.*

## Git commit convention
```
feat(ft-01): implement user registration and JWT login
fix(ft-05): correct tutorial status transition on manager reject
chore: add EF migration for VipSubscription expiry index
```
Branch: `feature/FT-XX-short-description`. PR cần ít nhất 1 reviewer, build phải pass trước khi merge. Phân công theo tuần xem `docs/MVP_SCOPE.md` mục 4.

## Do not
- Không đặt business logic trong Controller.
- Không dùng MediatR hay pipeline library nào khác.
- Không hard-delete content — dùng `IsDeleted` flag hoặc status transition.
- Không trả raw EF entity từ API — luôn map qua DTO.
- Không bỏ qua blocked word check ở bất kỳ endpoint ghi content nào.
- Không tự ý code FT thuộc mục WON'T-HAVE trong `docs/MVP_SCOPE.md`, kể cả khi BRD mô tả đầy đủ.
- Endpoint webhook `POST /api/v1/webhooks/sepay` PHẢI verify signature trước khi xử lý bất kỳ logic nào — không tin payload chưa verify.
- Không tạo lại bất kỳ code/entity nào liên quan `FamilyProject*`, `FamilySubscription`, `Ad*` (AdCampaign/AdBanner/AdImpression/AdClick/AdPlacement) — đã bị xoá khỏi codebase theo quyết định chốt scope, không thuộc BRD v5.0.
