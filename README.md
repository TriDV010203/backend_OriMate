# OriMate — Origami Community Platform (Backend)

Nền tảng học gấp giấy origami + cộng đồng. Đồ án tốt nghiệp SEP490_G69.

## Tài liệu liên quan (đọc theo thứ tự này)
1. `docs/CLAUDE.md` — pattern code, quy tắc cho AI coding agent (Command/Query/Handler)
2. `docs/BE_ARCHITECTURE.md` — kiến trúc solution, layer, request flow
3. `docs/BE_PROJECT_RULES.md` — coding convention, anti-pattern, DI setup, Git workflow
4. `docs/FT_MAPPING_v5.md` — mapping tính năng (FT) ↔ feature folder ↔ entity
5. `docs/MVP_SCOPE.md` — **phạm vi đang code (Must/Should/Won't-have)** — đọc trước khi bắt đầu bất kỳ task nào
6. `docs/Origami_ERD_Design_v3.docx` — database schema

## Yêu cầu môi trường
- .NET 8 SDK
- SQL Server 2022 (local hoặc Docker)
- (Tuỳ chọn) SQL Server Management Studio / Azure Data Studio

## Cài đặt lần đầu

```bash
git clone <repo-url>
cd backend
dotnet restore
```

### 1. Cấu hình `appsettings.Development.json`
Copy file mẫu và điền giá trị thật (KHÔNG commit file này — đã có trong `.gitignore`):
```bash
cp OrigamiPlatform.API/appsettings.Example.json OrigamiPlatform.API/appsettings.Development.json
```
Điền:
- `ConnectionStrings:Default` — connection string SQL Server local
- `Jwt:Key` — chuỗi bí mật bất kỳ ≥32 ký tự cho môi trường dev
- `Email:*` — Gmail SMTP + App Password (không dùng mật khẩu Gmail thật, tạo App Password riêng)
- `Cloudinary:*` — lấy từ dashboard Cloudinary (Cloud name/API Key/API Secret)

### 2. Tạo database + chạy migration
```bash
cd OrigamiPlatform.API
dotnet ef database update
```

### 3. Seed dữ liệu mẫu (Category, BlockedWord, tài khoản Admin/Manager)
Xem `docs/SEED_DATA.md` — chạy script hoặc gọi `SeedData.SeedAsync()` theo hướng dẫn trong đó. **Bắt buộc làm bước này trước khi test bất kỳ luồng nào liên quan Tutorial** (Tutorial cần `CategoryId` hợp lệ mới tạo được).

### 4. Chạy project
```bash
dotnet run --project OrigamiPlatform.API
```
Swagger: `https://localhost:{port}/swagger`

## Chạy test
```bash
dotnet test
```

## Quy tắc trước khi push code
- Đọc `docs/MVP_SCOPE.md` — chỉ code đúng phạm vi Must-have/Should-have đang mở
- Theo đúng pattern Command/Query/Handler ở `docs/BE_PROJECT_RULES.md` mục 5
- Build không warning trước khi tạo PR
- Không tự tạo lại bất kỳ code nào liên quan `FamilyProject*`/`Ad*` — đã bị loại khỏi scope

## Cấu trúc solution
```
OrigamiPlatform.API/            — Controllers, Middleware, Program.cs
OrigamiPlatform.Application/    — Commands/, Queries/, DTOs/, Validators/, Interfaces/
OrigamiPlatform.Domain/         — Entities, Enums, Exceptions
OrigamiPlatform.Infrastructure/ — EF Core, Repositories, Email/Storage/Jwt Service, Background Jobs
OrigamiPlatform.Tests/          — xUnit + Moq
docs/                           — toàn bộ tài liệu thiết kế/quy tắc
```
