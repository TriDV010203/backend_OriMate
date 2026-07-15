# MVP_SCOPE.md (v2) — OriMate, phạm vi code 3 tuần

## References
- `FT_MAPPING_v5.md` — mapping FT ↔ feature/entity/actor
- `CLAUDE.md` — pattern code (Command/Query/Handler)
- Team: 5 người (2 FE + 3 BE, có dùng AI hỗ trợ code)

⚠️ **v2 thay thế hoàn toàn v1** — sau khi audit `Application/` bằng `tree /F`, thực tế code đã có sẵn khác đáng kể so với giả định ban đầu. File này dựa trên trạng thái thật, không dựa trên BRD suy diễn nữa.

⚠️ **Quy tắc cho AI coding agent:** chỉ code các mục ở **1. VIỆC CẦN LÀM NGAY** và **2. MUST-HAVE CÒN THIẾU** trước. Không động vào **3. SHOULD-HAVE** cho đến khi 2 mục trên Done. Không code bất kỳ FT nào ở mục **5. WON'T-HAVE**.

---

## 0. Audit — trạng thái thực tế (tính đến thời điểm chốt v2)

| Feature | Trạng thái | Ghi chú |
|---|---|---|
| Auth (FT-01, FT-02) | ✅ Đã code đủ | Login/Register/VerifyEmail/ResendVerification/Forgot/Reset/ChangePassword/RefreshToken/Logout — Command/Handler đầy đủ |
| Achievements (FT-19) | ✅ Đã code đủ | Create/Update/Delete + GetUserAchievements |
| Comments (FT-13) | ✅ Đã code | Add/Delete + GetComments |
| CommunityPosts / Likes (FT-12) | ✅ Đã code | Create post, Toggle like, GetCommunityFeed |
| Follows (FT-13) | ✅ Đã code | ToggleFollow |
| Notifications (FT-13) | ✅ Đã code | Mark read (all/single) + GetNotifications |
| Reports (FT-12/14) | ✅ Đã code | Submit/Handle + GetPendingReports |
| Wishlists | ✅ Đã code | Bonus — không nằm trong Must-have gốc nhưng đã có sẵn, giữ luôn |
| TutorialProgress (FT-09) | ✅ Đã code | Complete/UncompleteStep + GetTutorialProgress |
| Users/Profile (FT-15) | ✅ Đã code | UpdateProfile + GetCreatorProfile |
| Journals (FT-21) | ✅ Đã code VÀ đã bật route thật (JournalsController + DI đầy đủ) | Đã xác nhận qua audit ModelSnapshot 2026-07 — quyết định "không bật route" trước đây chưa từng được thực thi. Chính thức chuyển sang Should-have Done, xem mục 3 |
| **Tutorials — Write (FT-04, 05, 07)** | 🔴 Cần refactor | Đang chạy qua `TutorialService` (Service pattern cũ) — quyết định refactor sang Command/Handler |
| **Tutorials — Read (FT-06, 08)** | ✅ Đã migrate | `GetTutorialsQuery`, `GetTutorialBySlugQuery` đã theo Command/Query |
| **AdminConfiguration (FT-03)** | 🔴 Cần refactor | Đang chạy qua `AdminConfigService` — quyết định refactor sang Command/Handler |
| **Stuck button (FT-10)** | ⚪ Chưa code | Không thấy feature `StuckThread`/`Stuck` nào trong Commands/Queries |
| **VIP Subscription (FT-16, 17)** | ⚪ Chưa code gì | `Commands/Subscriptions/` rỗng — chỉ có sẵn `IVipSubscriptionRepository` interface. **Đây là must-have chưa động tới** |
| Moderation nâng cao (FT-14 phần CTV) | ⚪ Chưa code | Folder `Commands/Moderation/` rỗng |
| FamilyProjects (scope cũ) | 🗑️ Cần xoá | Đã code khá đủ (6 Command/Handler) — **xoá theo quyết định đã chốt** |
| AdCampaigns / Ads (scope cũ) | 🗑️ Cần xoá | Đã code khá đủ (4 Command/Handler + 6 Query/Handler) — **xoá theo quyết định đã chốt** |
| Clan, DailyQuest, Streak, HatGap, Shop | ⚪ Chưa code | Đúng như dự kiến — thuộc Should-have, làm sau khi Must-have xong |

---

## 1. VIỆC CẦN LÀM NGAY — trước khi chạm vào bất kỳ FT mới nào (Ngày 1-2, Tuần 1)

| # | Việc | Ước lượng | Người phù hợp |
|---|---|---|---|
| 1 | **Xoá code FamilyProjects**: `Commands/FamilyProjects/`, `DTOs/FamilyProjects/`, `IFamilyProjectRepository.cs`, `IFamilySubscriptionRepository.cs`, entity `FamilyProject*`/`FamilySubscription`, DI registration, controller/route liên quan (nếu có ở API layer) | ~0.5 ngày | BE3 |
| 2 | **Xoá code AdCampaigns/Ads**: `Commands/AdCampaigns/`, `Queries/AdCampaigns/`, `DTOs/AdCampaigns/`, `IAdCampaignRepository.cs`, entity `Ad*`, `AdBudgetDepletionJob`, DI registration, controller/route | ~0.5 ngày | BE3 |
| 3 | **Xoá migration liên quan** đến 2 nhóm bảng trên (nếu đã apply lên DB dev) — tạo migration mới drop bảng, KHÔNG sửa tay migration cũ | ~0.5 ngày | Người đang giữ quyền tạo migration (theo Git Workflow, chỉ 1 người/lần) |
| 4 | **Refactor `TutorialService` → Command/Handler**: tách thành `CreateTutorialCommand/Handler`, `SubmitTutorialCommand/Handler`, `ReviewTutorialCommand/Handler` (Publish/RejectNeedChanges/Remove — theo đúng 1 vòng Manager, BR-TUT-01), `EditTutorialCommand/Handler` (working copy). Giữ nguyên `DTOs/Tutorials/*Dto.cs` cho phần Read đã có; phần Write đổi từ `Request/Response` sang convention Command/Dto cho khớp phần còn lại | ~1 - 1.5 ngày | BE1 |
| 5 | **Refactor `AdminConfigService` → Command/Handler**: `CreateCategoryCommand`, `UpdateCategoryCommand`, `CreateBlockedWordCommand`, `AssignRoleCommand`, `RemoveRoleCommand`, `SuspendUserCommand` + Handler tương ứng. Giữ nguyên các Validator FluentValidation đã có (`Validators/` của AdminConfiguration) — chỉ đổi target sang Command | ~0.5 - 1 ngày | BE2 |

**Tổng ước lượng: ~2.5 - 3.5 ngày** trên 3 người BE, làm song song → tương đương ~1 ngày lịch nếu chia đúng người. Đây là phần "nợ kỹ thuật" phải trả trước, không tính vào thời gian làm feature mới.

---

## 2. MUST-HAVE CÒN THIẾU (làm sau khi mục 1 xong)

| FT | Tên | Việc cần làm | Người phù hợp |
|---|---|---|---|
| FT-16 | VIP Subscription | **Giai đoạn 1 (trong 3 tuần):** xác nhận thủ công — `CreateTransactionCommand`, `ConfirmPaymentCommand` (Admin xác nhận), `SubscribeCommand` kích hoạt VIP khi Transaction Confirmed, `GetMySubscriptionsQuery`. Ước lượng lại về **~1 - 1.5 ngày** (như bản gốc). Thiết kế `Transaction` đủ field để giai đoạn 2 (SePay) cắm thêm mà không đổi schema — xem ERD | BE3 |
| FT-17 | Creator dashboard | `GetCreatorRevenueQuery` — chỉ cần tổng subscriber + tổng transaction confirmed | BE3 |

**Giai đoạn 2 — SePay webhook:** KHÔNG nằm trong 3 tuần này, làm sau khi có hạ tầng public (xem `docs/DEPLOYMENT.md`). Không chặn tiến độ MVP, không cần tài khoản sandbox/ngrok ngay bây giờ.
| FT-10 | Stuck button | `CreateStuckThreadCommand`, dùng lại `Comment` (TargetType=StuckThread) đã có sẵn cho phần reply | BE1 (sau khi xong refactor Tutorials, vì cùng domain) |
| FT-14 (CTV) | Moderation cơ bản | `DeleteViolatingCommentCommand` (CTV), review logic đã một phần nằm trong Reports — chỉ cần bổ sung quyền CTV | BE2 |

---

## 3. SHOULD-HAVE — chỉ làm nếu mục 1+2 xong sớm, ưu tiên đơn giản hoá tối đa

| FT | Tên | Đơn giản hoá |
|---|---|---|
| FT-21 | Folding Journal | ✅ **Đã Done** — code + route đã live thật (JournalsController), không cần làm gì thêm. Đã có AC/NAC trong SRS Part 3. Chỉ cần FE build UI nếu muốn dùng |
| FT-22 | Clan | Chỉ create/join/invite/leave |
| FT-26, FT-27 | Streak + Daily Quest | 1 pool duy nhất |
| FT-28 | Hạt Gấp | Chỉ sink Streak Freeze |
| FT-18 | Shop affiliate | Rất rẻ — ưu tiên làm nếu có slot trống, vì gần như chỉ là CRUD link |
| FT-11 | Tutorial variants | Nếu kịp |
| FT-08 | SEO & feed ranking nâng cao | Nếu kịp |

---

## 4. WON'T-HAVE — không code, không bật route

| FT | Tên | Ghi chú |
|---|---|---|
| FT-20 | Personal Milestone | Chưa code, không code trong 3 tuần |
| FT-23 | Weekly Challenge & pairwise | Rủi ro kỹ thuật cao |
| FT-24 | Clan Quest & League | Phụ thuộc FT-23 |
| FT-29/30/31/33 | Onboarding đầy đủ, Re-engagement, Discovery nâng cao, Learning Path | Không có trong 3 tuần |

---

## 5. Phân công 3 tuần (cập nhật)

| | Tuần 1 | Tuần 2 | Tuần 3 |
|---|---|---|---|
| **BE1** | Refactor Tutorials → Command/Handler (mục 1.4) | FT-10 Stuck button + FT-11 (nếu kịp) | Buffer/test/fix bug FE |
| **BE2** | Refactor AdminConfig → Command/Handler (mục 1.5) | FT-14 Moderation CTV + hỗ trợ Should-have (Clan/Quest) | Buffer/test/fix bug FE |
| **BE3** | Xoá FamilyProjects + AdCampaigns/Ads (mục 1.1-1.3) | FT-16, FT-17 VIP Subscription (thanh toán thủ công) | FT-18 Shop (nếu kịp) + buffer/test |

- Cuối tuần 2: nếu 3 người đều xong Must-have, dồn lực làm chung Should-have quan trọng nhất (Clan cơ bản + Streak/Quest rút gọn).
- Tuần 3 luôn dành ≥1-1.5 ngày cuối cho buffer, review PR chéo, Definition of Done.

## 6. Điều kiện mở rộng scope

Chỉ bổ sung FT từ mục 4 nếu: (a) mục 1+2 Done đầy đủ theo Definition of Done, VÀ (b) còn ≥3 ngày buffer trước hạn, VÀ (c) cả 3 BE đồng ý.
