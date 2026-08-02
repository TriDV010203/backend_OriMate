# SEED_DATA.md — Dữ liệu mẫu cho môi trường dev

## Vì sao cần file này
Không có Category thì không ai tạo được Tutorial (FK bắt buộc). Không có tài khoản Admin/Manager thì không test được luồng duyệt tutorial, quản lý category/blocked word. Đây là bước dev nào cũng cần làm **1 lần** sau khi `dotnet ef database update`.

## Cách dùng `SeedData.cs`

`OrigamiPlatform.Infrastructure/Persistence/SeedData.cs` đã dùng `IPasswordHasher` thật (`Application/Interfaces/`) và đã được gọi sẵn trong `Program.cs`, **chỉ chạy khi** `app.Environment.IsDevelopment()`:

```csharp
if (app.Environment.IsDevelopment())
{
    using var seedScope = app.Services.CreateScope();
    var seedContext = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedHasher = seedScope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await SeedData.SeedAsync(seedContext, seedHasher);
}
```

Chạy `dotnet run` — lần đầu sẽ tự seed, các lần sau tự bỏ qua (đã check `AnyAsync()`). Không cần chỉnh gì thêm.

## Tài khoản seed (chỉ dùng dev, KHÔNG dùng khi deploy thật)

| Email | Password | Role |
|---|---|---|
| `admin@orimate.dev` | `Admin@123` | Admin |
| `manager@orimate.dev` | `Manager@123` | Manager |

## Category seed
5 category cơ bản: Động vật, Hoa & Thực vật, Đồ vật & Hộp, Nhân vật & Trang phục, Kỹ thuật cơ bản. Đủ để tạo Tutorial test, team có thể thêm qua API `AdminConfiguration` sau khi có tài khoản Admin.

## Blocked word
Chỉ seed 3 từ placeholder (`spamword1`, `spamword2`, `testbadword`) để xác nhận `BlockedWordMiddleware` hoạt động đúng cơ chế. **Cần bổ sung danh sách từ cấm thật** trước khi dùng cho demo/deploy — không tự generate danh sách từ nhạy cảm, team tự tổng hợp theo ngữ cảnh cộng đồng thật.

## Reset lại seed (nếu cần seed lại từ đầu)
```sql
DELETE FROM UserRoles WHERE UserId IN (SELECT Id FROM Users WHERE Email IN ('admin@orimate.dev','manager@orimate.dev'));
DELETE FROM Users WHERE Email IN ('admin@orimate.dev','manager@orimate.dev');
DELETE FROM BlockedWords WHERE Word IN ('spamword1','spamword2','testbadword');
-- Category không nên xoá nếu đã có Tutorial tham chiếu — kiểm tra trước khi xoá
```
