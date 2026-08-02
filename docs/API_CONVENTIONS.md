# API_CONVENTIONS.md — OriMate

File này là **hợp đồng giữa FE và BE** — không thuộc về riêng team nào. Cả 2 team cùng đọc, cùng tuân theo. Khi 1 team đổi gì ảnh hưởng đến file này, phải báo team kia trước khi merge, không âm thầm đổi.

---

## 1. Base URL & versioning
```
https://<host>/api/...
```
Mọi endpoint đều có prefix `/api/`. Team đã bỏ versioning theo path (`/v1/`, `/v2/`) — khi có breaking change, xử lý qua thoả thuận trực tiếp với FE (báo trước trong group chat) thay vì tạo route version mới.

## 2. Format response

**Thành công — trả DTO trực tiếp, KHÔNG có wrapper:**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "title": "Hạc giấy cơ bản",
  "status": "Published"
}
```

**Danh sách có phân trang:**
```json
{
  "items": [ { "...": "..." } ],
  "page": 1,
  "pageSize": 20,
  "totalItems": 134,
  "totalPages": 7
}
```

**Lỗi — luôn theo shape này, bất kể lỗi loại gì:**
```json
{ "error": "Human-readable message" }
```
FE parse lỗi bằng cách check `response.data.error`, không có field nào khác trong object lỗi (không có `code`, không có `details` — nếu sau này cần thêm, phải báo trước khi đổi).

## 3. HTTP status code mapping

| Status | Khi nào | Ví dụ message |
|---|---|---|
| 200 | Thành công (GET/PUT/PATCH) | — |
| 201 | Tạo mới thành công (POST) | — |
| 400 | Vi phạm business rule / validate sai | "Title must be 5-150 characters." |
| 401 | Chưa đăng nhập / token hết hạn | "Unauthorized." |
| 403 | Đã đăng nhập nhưng không đủ quyền | "Only the author can submit this tutorial." |
| 404 | Không tìm thấy resource | "Tutorial not found." |
| 500 | Lỗi hệ thống không lường trước | "An unexpected error occurred." |

FE nên xử lý 401 riêng (redirect login / refresh token), 400/403/404 hiện message trực tiếp từ `error`, 500 hiện message chung chung (không hiện raw error cho user).

## 4. Auth

```
Header: Authorization: Bearer <access_token>
```
- Access token hết hạn: 60 phút (`Jwt:AccessTokenExpiryMinutes`)
- Refresh token: 30 ngày, gọi `POST /api/auth/refresh-token` khi access token hết hạn (401)
- Logout: `POST /api/auth/logout` — thu hồi refresh token hiện tại

## 5. Danh sách endpoint theo feature

Không liệt kê chi tiết ở đây để tránh trùng lặp/lệch với code thật — nguồn chính xác nhất luôn là **Swagger** (`/swagger` khi BE chạy dev). File này chỉ quy định *format chung*, không phải danh sách endpoint.

FE lấy danh sách endpoint mới nhất qua Swagger, hoặc theo FT đang làm tra ở `docs/FT_MAPPING_v5.md` (feature folder ↔ FT) để biết đang chờ API nào.

## 6. Naming convention JSON field

- `camelCase` cho mọi field (do `System.Text.Json` mặc định serialize theo camelCase từ property PascalCase của C#)
- Ngày giờ: ISO 8601 UTC, ví dụ `"2026-07-10T08:30:00Z"` — FE tự convert sang giờ local (GMT+7) khi hiển thị
- Enum trả về dạng string (không trả số), ví dụ `"status": "Published"` không phải `"status": 2`

## 7. Upload file (ảnh cover, ảnh step, ảnh achievement...)

- Upload qua Cloudinary — FE **không** upload thẳng lên Cloudinary từ client, mà gửi file lên BE endpoint tương ứng (ví dụ `POST /api/tutorials/{id}/cover-image`), BE forward lên Cloudinary rồi trả về `url`.
- Giới hạn dung lượng: theo BR từng feature (ví dụ Achievement ảnh ≤10MB) — tra `CLAUDE.md`/BR table nếu cần con số chính xác.

## 8. Rate limit / pagination mặc định

- `page` mặc định = 1, `pageSize` mặc định = 20, tối đa `pageSize` = 100 (BE tự cap, không cần FE validate)

## 9. Khi API đổi (breaking change)

Bên gây thay đổi phải:
1. Báo trong group chat trước khi merge, không để FE tự phát hiện qua lỗi 400/404 bất ngờ.
2. Cập nhật Swagger + note ngắn ở PR description.
3. Nếu đổi shape response ảnh hưởng nhiều màn hình FE, ưu tiên thêm field mới thay vì đổi/xoá field cũ khi có thể.

---

## Đặt file này ở đâu

BE và FE là 2 repo tách biệt, không có chỗ "dùng chung" tự nhiên — nên **copy file này vào cả 2 repo**:
```
backend_OriMate/docs/API_CONVENTIONS.md
frontend_OriMate/docs/API_CONVENTIONS.md   (hoặc README/docs tương đương bên FE)
```
Chọn **BE repo làm nguồn chính** (vì BE là bên định nghĩa contract) — khi cần sửa, sửa ở BE trước, rồi copy nguyên văn sang FE, đừng để 2 file rời rạc rồi lệch nhau. Nếu về sau thấy bất tiện, có thể cân nhắc tạo 1 repo riêng `orimate-docs` chỉ chứa file dùng chung (API_CONVENTIONS, ERD...), cả 2 repo submodule/link vào — nhưng với timeline 3 tuần, copy tay 1 file ngắn thế này đơn giản hơn.
