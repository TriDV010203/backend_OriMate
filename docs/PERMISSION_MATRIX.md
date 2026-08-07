# Permission Matrix — OriMate

> Đối chiếu lại bản gốc (Excel) với code thực tế tính đến **2026-08-07**:
> - Backend: `[Authorize(Roles=...)]` trên từng controller trong `OrigamiPlatform.API/Controllers/*.cs`
> - Frontend: route/component thực có trong `orimate-web/app/**`
> - Tham chiếu chéo: `docs/FT_FE_SC_AsImplemented.md`, `docs/MVP_SCOPE.md`
>
> Các sai lệch so với bản gốc được ghi rõ trong cột **Ghi chú** của từng dòng, không sửa âm thầm.

## Legend

| Code | Ý nghĩa |
|---|---|
| **F** | Full — toàn quyền |
| **E** | Edit — sửa/xử lý nhưng có giới hạn (vd. cần lý do, không áp dụng lên Admin...) |
| **R** | Read — chỉ xem |
| **O** | Own — chỉ trên tài nguyên của chính mình |
| **—** | Không có quyền |
| **Inter 3** | Backend đã code xong nhưng **FE chưa có giao diện** — dự kiến làm ở Iteration 3 |

## Thay đổi cấu trúc so với bản gốc

1. **Bỏ cột "Advertising Partner"** — role này chưa từng tồn tại trong code (`UserRoleType` chỉ có `User / ContributorReviewer / Manager / Admin`), và toàn bộ domain quảng cáo (`AdCampaigns`, `AdBanners`, `AdPlacements`, `AdClicks`, `AdImpressions`) **đã bị xoá khỏi database** bằng migration `DropFamilyProjectAndAdTables` theo quyết định chốt trong `MVP_SCOPE.md`. Đây không phải "chưa làm ở FE" mà là đã loại khỏi phạm vi dự án — nên bỏ hẳn phần **Advertising Management** khỏi ma trận thay vì đánh dấu Inter 3.
2. **"Family Activities" → "Clan Activities"** — `FamilyProject`/`FamilySubscription` cũng bị xoá cùng đợt với Ad. Tính năng gần nhất còn tồn tại trong code là **Clan** (`ClanController`), nhưng khác thiết kế (không có "project" chung, không giới hạn 5 project). Đã thay toàn bộ mục này bằng Clan thật, đánh dấu **Inter 3** vì BE xong nhưng FE chưa có bất kỳ route/màn hình nào (`orimate-web` không có chữ "clan" ở đâu cả).
3. **"Creator" không phải một role riêng** — đây là persona của **User** đã đăng tutorial, không phải giá trị trong `UserRoleType`. Cột "User/Creator" giữ nguyên như bản gốc vì đã đúng.
4. **Gán role là thay thế toàn bộ, không cộng dồn** (`AssignRoleCommand`): 1 tài khoản chỉ giữ đúng 1 role tại một thời điểm. Tuy nhiên phần lớn API ghi nội dung cá nhân (tutorial draft, post, comment, wishlist, journal, clan...) chỉ gắn `[Authorize]` chung (yêu cầu đăng nhập, không giới hạn role cụ thể) — quyền thực tế đến từ kiểm tra **chủ sở hữu** trong handler, không phải role. Vì vậy về lý thuyết một tài khoản Manager/Admin/CTV vẫn tự tạo được tutorial/bài đăng của riêng họ như User thường, dù không phải luồng nghiệp vụ chính.

---

## 1. User Account & Authentication

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Sign up / Log in / Reset password | F | F | F | F | F | |
| Search / xem / quản lý danh sách account | — | — | — | — | F | `AdminController` toàn bộ class là `[Authorize(Roles="Admin")]` — **Manager không có quyền này** (bản gốc ghi Manager=R, sai) |
| Tạo account Manager / Reviewer (gán role) | — | — | — | — | F | Không có bước "duyệt CTV" riêng như bản gốc ghi ("Approve Reviewer") — Admin gán thẳng role `ContributorReviewer` giống mọi role khác, một thao tác duy nhất |
| Assign / gỡ role | — | — | — | — | F | Không tự gỡ role Admin của chính mình |
| Suspend / activate account (trực tiếp, ngoài luồng report) | — | — | — | — | F | Manager không có quyền này ở `AdminController` |
| Suspend account qua xử lý report (`HandleReport`) | — | — | — | E | F | Không áp dụng được lên tài khoản đang có role Admin (kể cả chính mình) |

## 2. Origami Tutorial

| Action | Guest | User/Creator | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Create / edit tutorial (draft) | — | O | O | — | — | 3-30 bước, qua blocked-word check |
| Submit tutorial for review | — | O | O | — | — | |
| Duyệt tutorial (Publish / Reject cần sửa / Remove) | — | — | — | F | F | **1 vòng duy nhất** — không có "first-pass CTV review" như bản gốc; CTV không tham gia review tutorial (endpoint review chỉ `Roles="Admin,Manager"`) |
| Request revision (lý do ≥10 ký tự) | — | — | — | F | F | Không terminal — Creator sửa & nộp lại được nhiều lần |
| Final approval / Publish | — | — | — | F | F | |
| Publish directly (không qua hàng đợi) | — | — | — | F | F | Chỉ áp dụng cho tutorial Admin/Manager tự tạo qua `POST /admin` (gắn `IsOfficial`), không áp dụng cho tutorial của Creator thường |
| Edit tutorial đã publish | — | O | O | F | F | Creator tạo "working copy" song song, Manager/Admin duyệt merge; Admin/Manager còn sửa trực tiếp bất kỳ tutorial nào qua `PUT /admin` (bỏ qua working-copy) |
| Remove published tutorial | — | — | — | F | F | Terminal, không sửa/nộp lại được |
| View approval history | — | O (lý do reject của chính mình) | — | F | F | Không có endpoint xem lịch sử riêng — hiển thị kèm trong trang quản trị `/admin/{id}` |

## 3. Library & Content Viewing

| Action | Guest | User/Creator | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| View list / search / filter tutorials | R | R | R | R | R | |
| View free tutorial details | R | R | R | R | R | |
| View VIP preview (3 bước đầu) | R | R | R | R | R | `VipFreePreviewStepCount` |
| View full VIP content | — | R (cần subscription active) | R (cần subscription active) | R (cần subscription active) | R (cần subscription active) | Khoá thật ở server (`Description=""`, `ImageUrl=null`), không có ngoại lệ role — Admin/Manager cũng phải có subscription mới xem đủ nội dung |

## 4. Community Interaction

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Create community post | — | F | F | F | F | Chỉ cần đăng nhập, không role-gate |
| Like / Comment | — | F | F | F | F | |
| Follow author / Creator | — | F | F | F | F | |
| Delete own comment/post | — | O | O | O | O | |
| Delete violating comment (trực tiếp, không qua report) | — | — | E | F | F | `ModerationController`, cần lý do ≥10 ký tự |
| Report post / comment | — | F | F | F | F | |
| Handle report queue (Dismiss / Remove content / Suspend) | — | — | — | F | F | **CTV không thấy và không xử lý hàng đợi report** (bản gốc ghi CTV=E, sai — `ReportsController` chỉ `Roles="Manager,Admin"`) |

## 5. Achievements & Personal Journal

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Save to Wishlist | — | O | O | O | O | |
| Mark as "Completed" | — | O | O | O | O | |
| Upload achievement photo/video | — | O | O | O | O | Mặc định hiện là Public khi tạo — lệch với business rule "mặc định Private", xem `FT_FE_SC_AsImplemented.md` mục Phụ lục #6 |
| Manage / set journal privacy | — | O | O | O | O | |

## 6. Notifications

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Receive in-app notifications | — | F | F | F | F | |
| Receive email cho sự kiện quan trọng | — | F | F | F | F | |

## 7. Clan Activities *(thay "Family Activities" — Family Project đã bị xoá khỏi scope)*

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Tạo Clan & mời thành viên | — | O | O | O | O | **Inter 3** — BE có sẵn (`ClanController`), FE chưa có route/màn hình nào |
| Chấp nhận / từ chối lời mời | — | O | O | O | O | **Inter 3** — lời mời hết hạn sau 48h; mỗi user chỉ ở 1 Clan tại 1 thời điểm |
| Rời Clan (thành viên thường) | — | O | O | O | O | **Inter 3** |
| Chuyển quyền Owner trước khi rời | — | O | O | O | O | **Inter 3** — Owner không rời trực tiếp được nếu chưa chuyển quyền |
| Xem danh sách thành viên Clan | — | O | O | O | O | **Inter 3** |

## 8. Creator VIP Plans

| Action | Guest | User/Creator | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Bật/tắt bán VIP kênh | — | O | O | — | — | Giá **cố định theo nền tảng** — Creator **không** tự đặt giá 10/20/50k như bản gốc ghi; cần ≥5 tutorial Published (`BR-VIP-06`) |
| Mark tutorial as VIP | — | O | O | F | F | Cần `CreatorVipSettings.IsActive=true` |
| Subscribe to another Creator's VIP | — | F | F | F | F | Qua SePay tự động, xem mục Payments |
| View own channel's VIP subscriber list / revenue | — | O | O | — | F | Chỉ chủ kênh hoặc Admin xem được (chặn ở tầng authorization trong handler) — **Manager không có quyền xem doanh thu/subscriber của Creator** (bản gốc ghi Manager=— đã đúng, giữ nguyên) |

## 9. Payments

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Create order / make payment | — | F | F | F | F | Qua SePay: tạo Transaction + QR |
| Confirm payment | — | — | — | — | — | **Tự động qua webhook SePay** (verify chữ ký, khớp mã + số tiền, idempotent) — không còn thao tác xác nhận thủ công của Admin/Manager như thiết kế cũ trong bản gốc |
| View own transactions | — | O | O | O | O | |
| View all transactions / doanh thu toàn hệ thống | — | — | — | — | F | Chỉ Admin (`transactions`, `admin/revenue` đều `Roles="Admin"`) — **Manager không có quyền này** (bản gốc ghi Manager=R, sai) |

## 10. Moderation & Violation Reports

*(gộp với mục 4 — không tách riêng nữa vì cùng một cơ chế `ReportsController`/`ModerationController`, tránh trùng lặp như bản gốc)*

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| Report violating content | — | F | F | F | F | |
| Handle report queue (ignore / remove / suspend) | — | — | — | F | F | CTV không tham gia |
| Delete violating comment trực tiếp | — | — | E | F | F | |
| Lock violating account | — | — | — | E | F | Qua `HandleReport`, không áp dụng lên tài khoản Admin |
| Configure banned-words list | — | — | — | — | F | `AdminController` Admin-only — **Manager không có quyền** (bản gốc ghi Manager=E, sai) |
| Manage topic categories | — | — | — | — | F | Tương tự, Admin-only — **Manager không có quyền** (bản gốc ghi Manager=E, sai) |

## 11. Dashboard & Analytics

| Action | Guest | User | Contributor Reviewer | Manager | Admin | Ghi chú |
|---|---|---|---|---|---|---|
| System overview dashboard | — | — | — | — | — | **Chưa triển khai ở cả BE lẫn FE** — không có `DashboardController` hay endpoint tổng quan nào trong code hiện tại. Không đánh dấu Inter 3 vì đó là nhãn dành riêng cho "BE xong, FE chưa làm" |
| Personal channel analytics (Creator) | — | O | O | — | — | |
| View VIP revenue (của chính mình + Admin xem toàn hệ thống) | — | O | O | — | F | |

---

## Phần đã bỏ khỏi ma trận (không phải Inter 3 — đã loại khỏi scope dự án)

| Feature Area gốc | Lý do bỏ |
|---|---|
| **Advertising Management** (toàn bộ) + role **Advertising Partner** | Chưa từng có role này trong code; entity/bảng `AdCampaigns`, `AdBanners`, `AdPlacements`, `AdClicks`, `AdImpressions` đã bị xoá khỏi DB bằng migration `20260709173913_DropFamilyProjectAndAdTables.cs`, theo quyết định chốt trong `MVP_SCOPE.md` mục 0 và 1.2. |
| **Family Activities** bản gốc (Buy family plan, family project tối đa 5, share project to community) | `FamilyProjects`/`FamilySubscriptions` bị xoá cùng migration trên. Đã thay bằng **Clan Activities** (mục 7) — tính năng gần nhất còn tồn tại trong code, nhưng khác thiết kế hoàn toàn với "Family Project" cũ. |

## Các FT khác đang ở trạng thái Inter 3 (BE xong, FE chưa làm) — ngoài phạm vi bảng phân quyền

Không nằm trong logic phân quyền theo role (đều là hành động `O` — chủ sở hữu) nhưng đáng lưu ý vì cùng lý do "chưa có UI":
- Hỏi khi bị mắc ("Bí rồi" / `StuckThread`)
- Biến thể tutorial (variant)
- Mua Paper Pattern bằng Hạt Gấp
- First-run onboarding
- Gợi ý tutorial cá nhân hoá
- Tìm kiếm bằng hình ảnh

Chi tiết đầy đủ từng tính năng xem `docs/FT_FE_SC_AsImplemented.md`.
