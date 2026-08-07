# FE_SCREEN_ITERATION_PLAN.md — Đối chiếu màn hình FE với `orimate-web` hiện tại

> Nguồn đối chiếu: cấu trúc route thực tế trong `orimate-web/app/**` (đọc trực tiếp ngày 2026-08-07) + `FT_FE_SC_AsImplemented.md` / `FT_MAPPING_v5.md` trong repo này. Cột **Planned Iteration** giữ nguyên giá trị gốc cho các màn hình **đã có FE**; các màn hình **chưa có FE** được gán **Iteration 3**.

| Function / Screen | Type | Feature Group | Sub-Feature | Description | Complexity | Est. Effort (pts) | Planned Iteration | In Charge | Actual Effort (pts) | Status | Notes |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Home Page | Non-UI | Common | | Landing page / entry point | Medium | 5 | Iteration 2 | | | Not Started | Đã có FE — `app/page.tsx` |
| User Login | Screen | Common | | Email + password login with JWT | Simple | 3 | Iteration 1 | | | Not Started | Đã có FE — `app/(auth)/dang-nhap` |
| User Register | Screen | Common | | New account registration form | Simple | 3 | Iteration 1 | | | Not Started | Đã có FE — `app/(auth)/dang-ky` |
| Reset Password | Screen | Common | | Email OTP-based password reset flow | Medium | 5 | Iteration 3 | | | Not Started | Đã có FE — `app/(auth)/quen-mat-khau`, `app/(auth)/dat-lai-mat-khau`. Lưu ý: BE dùng reset-link qua email (token 1h), không phải nhập mã OTP — mô tả cần chỉnh lại cho khớp |
| User Authorization | Non-UI | Common | | Role-based access control (RBAC) | Complex | 8 | Iteration 1 | | | Not Started | Đã có BE + FE — FT-04 (gán/gỡ role, suspend/activate) đã dùng trong `AdminUsersPage` |
| User Profile | Screen | Common | | View and edit user profile | Simple | 3 | Iteration 3 | | | Not Started | Đã có FE — `app/ho-so`, `app/ho-so/chinh-sua` |
| Change Password | Screen | Common | | Change password from profile | Simple | 3 | Iteration 3 | | | Not Started | Đã có FE — `app/ho-so/doi-mat-khau` |
| Blog List | Screen | Public | | Paginated public blog listing | Medium | 5 | **Iteration 3** | | | Not Started | **Chưa có FE.** Không nằm trong 8 nhóm tính năng hiện tại (FE-01→FE-08 trong `FT_MAPPING_v5.md`) — không có route, controller hay entity "Blog" nào ở BE lẫn FE. Cần xác nhận có còn trong scope không trước khi lên kế hoạch chi tiết |
| Blog Details | Screen | Public | | Single blog post view with comments | Simple | 3 | **Iteration 3** | | | Not Started | **Chưa có FE.** Phụ thuộc Blog List ở trên |
| Post List | Screen | Marketing | | Marketing posts listing with filters | Medium | 5 | **Iteration 3** | | | Not Started | **Chưa có FE.** Khác với "Community Post" đã có ở `app/cong-dong` (FE-03) — đây là post dạng marketing/CMS, chưa tồn tại ở cả BE và FE |
| Post Details | Screen | Marketing | | Single marketing post detail view | Medium | 5 | **Iteration 3** | | | Not Started | **Chưa có FE.** Phụ thuộc Post List ở trên |
| Users List | Screen | System Admin | | Admin: list + search all users | Simple | 3 | Iteration 1 | | | Not Started | Đã có FE — `app/(dashboard)/admin/users` (`AdminUsersPage`) |
| User Details | Screen | System Admin | | Admin: view + edit single user record | Simple | 3 | **Iteration 3** | | | Not Started | **Chưa có FE.** `AdminUsersPage` hiện chỉ có danh sách + thao tác inline (gán role, suspend/activate); chưa có màn hình/route chi tiết riêng cho từng user |

## Tóm tắt

- **Đã có FE, giữ nguyên iteration gốc:** Home Page, User Login, User Register, Reset Password, User Authorization, User Profile, Change Password, Users List.
- **Chưa có FE → đưa vào Iteration 3:** Blog List, Blog Details, Post List (Marketing), Post Details (Marketing), User Details (Admin).
- **Cần lưu ý riêng:** Blog và Post (Marketing) không nằm trong bất kỳ FE-01→FE-08 nào đã định nghĩa ở `FT_MAPPING_v5.md` — trước khi đưa vào Iteration 3 chính thức, nên xác nhận lại với Product xem đây có phải scope mới cần bổ sung FE/FT hay không.
