# DEPLOYMENT.md — OriMate Backend trên Azure

## Vì sao cần làm sớm (khi bắt đầu Giai đoạn 2 — SePay)
FT-16 giai đoạn 1 (3 tuần đầu) dùng xác nhận thủ công, **không cần deploy thật** — xem `MVP_SCOPE.md`. File này chỉ cần dùng khi team chuyển sang Giai đoạn 2 (tích hợp SePay webhook thật), vì lúc đó cần 1 endpoint public HTTPS để SePay gọi vào. Không phải việc gấp của 3 tuần đầu, nhưng nên đọc trước để biết cần chuẩn bị gì khi tới lúc.

## Điều kiện: Azure for Students
- Đăng ký tại https://azure.microsoft.com/free/students — xác thực bằng email trường (`.edu.vn`). Không cần thẻ tín dụng.
- Nhận **$100 credit**, hạn dùng 12 tháng, kèm 1 số dịch vụ free tier vĩnh viễn.
- Chỉ cần **1 người trong team đăng ký** (dùng subscription đó cho cả team), hoặc mỗi người đăng ký riêng rồi add nhau vào chung 1 subscription qua **Azure Portal → Subscriptions → Access control (IAM) → Add role assignment**.

⚠️ **Quản lý credit cẩn thận** — $100 nghe nhiều nhưng SQL Database + App Service chạy 24/7 trong 3 tuần có thể tốn kha khá nếu chọn tier cao. Xem mục "Quản lý chi phí" cuối file.

---

## 1. Tạo resource

### a) Resource Group (gom mọi resource vào 1 chỗ, dễ xoá sạch khi xong)
Azure Portal → **Create a resource** → **Resource Group** → đặt tên `orimate-rg` → chọn region gần nhất (`Southeast Asia`).

### b) Azure SQL Database
1. **Create a resource** → **SQL Database**
2. Resource group: `orimate-rg` | Database name: `OrimateDb`
3. **Create new server**: đặt admin username/password riêng (KHÔNG dùng chung với password code) — lưu lại, cần cho connection string
4. Compute + storage: chọn **Serverless** (tier `General Purpose - Serverless`, 0.5-1 vCore) — tự động scale xuống khi không dùng, tiết kiệm credit hơn tier cố định
5. Networking: **Allow Azure services to access this server** = Yes. Thêm rule cho phép IP hiện tại của máy dev để test connect từ local nếu cần.

### c) Azure App Service (chạy API)
1. **Create a resource** → **Web App**
2. Resource group: `orimate-rg` | Name: `orimate-api` (URL sẽ là `orimate-api.azurewebsites.net` — cần tên chưa ai dùng, đổi nếu trùng)
3. Publish: **Code** | Runtime stack: **.NET 8 (LTS)** | OS: **Linux** (rẻ hơn Windows, không cần license)
4. Pricing plan: chọn **B1 Basic** (không dùng F1 Free — xem lý do ở mục Quản lý chi phí)

---

## 2. Cấu hình App Settings (KHÔNG commit appsettings.Development.json lên server)

Azure Portal → App Service `orimate-api` → **Settings → Environment variables** (hoặc "Configuration" tuỳ giao diện) → thêm từng key, map đúng theo cấu trúc `appsettings.Example.json`:

```
ConnectionStrings__Default = <connection string lấy từ SQL Database → Connection strings → ADO.NET>
Jwt__Key = <random secret riêng cho production, KHÁC key dùng ở dev>
Jwt__Issuer = OriMate.Api
Jwt__Audience = OriMate.Client
Email__SmtpHost = smtp.gmail.com
Email__SmtpPort = 587
Email__SmtpUser = <email thật>
Email__SmtpAppPassword = <app password thật>
Cloudinary__CloudName = <thật>
Cloudinary__ApiKey = <thật>
Cloudinary__ApiSecret = <thật>
SePay__ApiKey = <lấy từ SePay merchant dashboard sau khi đăng ký sandbox>
SePay__WebhookSecret = <dùng để verify X-SePay-Signature>
```
Lưu ý dấu `__` (2 dấu gạch dưới) thay cho `:` khi đặt tên biến môi trường trên Azure — ASP.NET Core tự map `Email__SmtpHost` thành `Email:SmtpHost`.

## 3. Chạy migration lên Azure SQL

Từ máy local, trỏ connection string sang Azure SQL rồi chạy:
```bash
dotnet ef database update --connection "<connection string Azure SQL>"
```
Sau đó chạy seed data (xem `SEED_DATA.md`) — nhớ đổi password seed Admin/Manager thành mật khẩu thật trước khi seed lên môi trường public, không để `Admin@123`.

## 4. Đăng ký webhook với SePay

1. Đăng ký tài khoản sandbox/merchant tại SePay (https://sepay.vn) — làm **song song** lúc dựng Azure, đừng chờ deploy xong mới đăng ký, vì duyệt tài khoản có thể mất thời gian.
2. Trong dashboard SePay, khai báo Webhook URL:
   ```
   https://orimate-api.azurewebsites.net/api/webhooks/sepay
   ```
3. Lấy `Webhook Secret` để verify signature — điền vào App Settings `SePay__WebhookSecret` ở bước 2.

## 5. CI/CD — tự động deploy khi merge vào `main`

Thêm job vào `.github/workflows/build.yml` đã có (hoặc tạo file riêng `deploy.yml`):
```yaml
  deploy:
    needs: build-and-test
    if: github.ref == 'refs/heads/main' && github.event_name == 'push'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '8.0.x'
      - run: dotnet publish backend/OrigamiPlatform.API -c Release -o ./publish
      - uses: azure/webapps-deploy@v3
        with:
          app-name: orimate-api
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```
`AZURE_WEBAPP_PUBLISH_PROFILE` lấy từ Azure Portal → App Service → **Get publish profile** (tải file .xml) → paste nguyên nội dung vào GitHub repo **Settings → Secrets and variables → Actions**.

---

## Quản lý chi phí — tránh cháy $100 credit trước khi demo

- **Không chọn F1 Free tier cho App Service** dù nghe "free" hấp dẫn — F1 có quota CPU rất thấp (60 phút/ngày), dễ bị chặn (lỗi 403) đúng lúc demo hoặc lúc SePay gọi webhook dồn dập. B1 Basic rẻ, ổn định hơn nhiều cho nhu cầu 3 tuần.
- **SQL Database Serverless tự động pause** khi không dùng — không cần tự tắt tay.
- Theo dõi credit tại **Azure Portal → Cost Management + Billing** hàng tuần, không để đến cuối mới nhìn.
- Sau khi bảo vệ đồ án xong, **xoá `orimate-rg`** (xoá cả resource group là xoá sạch mọi thứ bên trong) để không tốn thêm credit vô ích — hoặc dừng App Service nếu muốn giữ lại demo cho hồ sơ.

## Thứ tự làm trong tuần 1 (khuyến nghị)
1. 1 người đăng ký Azure for Students ngay hôm nay (song song lúc BE1/BE2 đang refactor).
2. Dựng Resource Group + SQL Database + App Service (khung rỗng, chưa cần deploy code thật) — ~1 buổi.
3. Đăng ký SePay sandbox song song.
4. Khi BE3 bắt đầu code FT-16, hạ tầng đã sẵn sàng để test webhook thật, không phải chờ.
