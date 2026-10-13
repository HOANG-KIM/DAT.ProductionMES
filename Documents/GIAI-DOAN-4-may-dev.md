# ProductionMES: Giai đoạn 4, việc cần làm trên máy dev

> Gửi cho: người phụ trách mã nguồn ProductionMES (máy dev có repo).
> Ngày gửi: 2026-10-13.
> Tài liệu gốc: `DEPLOY-ubuntu-vm-hyperv.md` (mục 6, Giai đoạn 4).

---

## 1. Bối cảnh

ProductionMES sẽ chạy trên một VM Ubuntu (`192.168.1.248`, domain nội bộ `https://mes.internal`):

```
Trình duyệt / Station.Wpf ──HTTPS──▶ Caddy :443 (trên VM) ──HTTP──▶ Kestrel 127.0.0.1:5000 (API)
```

**Caddy** là reverse proxy. Nó nhận HTTPS từ client, giải mã, rồi chuyển request vào API bằng **HTTP thường**.

**Vấn đề:** trong `src/ProductionMES.Api/Program.cs`, phần `AddAntiforgery` đặt
`Cookie.SecurePolicy = CookieSecurePolicy.Always`. Vì API nhận request qua HTTP, nó tưởng request gốc
không phải HTTPS. Kết quả là **mọi request POST/PUT lỗi antiforgery** (400/500).

**Cách sửa:** cho API đọc header `X-Forwarded-Proto: https` mà Caddy gửi kèm, bằng middleware
`ForwardedHeaders`. Không được nới lỏng `Secure`/`SameSite` của cookie, vì ADR-003 yêu cầu giữ nguyên.

---

## 2. Các bước thực hiện

### 2.1 Tạo nhánh riêng
Làm trên nhánh riêng, không gộp vào nhánh của user story khác:
```powershell
git checkout main          # hoặc nhánh chính mà team đang dùng
git pull
git checkout -b deploy/forwarded-headers
```

### 2.2 Sửa `src/ProductionMES.Api/Program.cs`

**a) Thêm `using`** ở đầu file:
```csharp
using Microsoft.AspNetCore.HttpOverrides;
```

**b) Đăng ký service**, đặt cùng chỗ với các `builder.Services...` khác (trước `builder.Build()`):
```csharp
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Caddy (reverse proxy cùng VM, gọi vào qua loopback, đã nằm trong KnownProxies mặc định) kết thúc HTTPS
    // rồi chuyển tiếp HTTP vào Kestrel — phải đọc X-Forwarded-Proto để API biết request gốc là HTTPS.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});
```

**c) Thêm middleware** ngay sau `var app = builder.Build();`. Dòng này phải **đứng trước mọi middleware
khác** (`UseExceptionHandler`, `UseHsts`, `UseHttpsRedirection`, `UseCors`, `UseAuthentication`, `UseAntiforgery`...):
```csharp
var app = builder.Build();

app.UseForwardedHeaders();

// ... các middleware hiện có giữ nguyên thứ tự ...
```

Ghi chú:
- Không cần khai báo `KnownProxies`/`KnownNetworks`. Caddy gọi API qua `127.0.0.1`, mà loopback đã được
  tin cậy theo mặc định. **Không** xoá danh sách mặc định (`KnownNetworks.Clear()`), vì như vậy bất kỳ ai cũng
  giả mạo được header.
- Nếu `Program.cs` có `app.UseHttpsRedirection()`, middleware này **bắt buộc** phải đứng trước nó. Nếu không,
  API sẽ redirect vòng lặp HTTP sang HTTPS sau Caddy.
- Khi chạy ở máy dev (không qua proxy), không có header `X-Forwarded-*` nên middleware không làm gì. Hành vi
  khi dev không thay đổi.

### 2.3 Build và test
```powershell
dotnet build
dotnet test               # nếu solution có project test
```
Chạy API ở local như bình thường, đăng nhập web-admin và thử một thao tác POST/PUT để chắc chắn không có gì
bị hỏng ở môi trường dev.

(Không test được trọn vẹn tình huống qua Caddy trên máy dev. Phần đó sẽ kiểm tra ở Giai đoạn 10 sau khi deploy.)

### 2.4 Commit và merge
```powershell
git add src/ProductionMES.Api/Program.cs
git commit -m "Thêm UseForwardedHeaders để chạy sau reverse proxy Caddy (deploy VM Ubuntu)"
git push -u origin deploy/forwarded-headers
```
Tạo Pull Request / merge theo quy trình của team.

---

## 3. Thông tin cần gửi lại cho người deploy

Tài liệu deploy được viết dựa trên một số giả định về code. Nhờ bạn kiểm tra trong repo rồi trả lời từng mục.
**Không gửi mật khẩu, JWT key hay secret thật.**

| # | Cần xác nhận | Xem ở đâu | Giả định hiện tại |
|---|---|---|---|
| 1 | **Connection string**: format đầy đủ, có tham số nào ngoài Server/Port/Database/User/Password không (`SslMode`, `CharSet`, `AllowPublicKeyRetrieval`...)? **Xoá mật khẩu trước khi gửi.** | `appsettings.json`, `appsettings.Development.json` | `Server=...;Port=3306;Database=ProductionMES;User=...;Password=...;` |
| 2 | Tên chính xác các key cấu hình | `appsettings.json` | `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `Cors:WebAdminOrigin`, `PackingTemplateStorage:RelativeFolderPath` |
| 3 | Trong mảng `Serilog:WriteTo`, sink `File` nằm ở **vị trí (index) thứ mấy**? | `appsettings.json` | index `1` (phần tử thứ 2) |
| 4 | Có endpoint `/health` không? Nó có kiểm tra kết nối DB không? | `Program.cs` (`MapHealthChecks`) | Có, trả về `Healthy` |
| 5 | `Program.cs` có `UseHttpsRedirection()` / `UseHsts()` không? | `Program.cs` | Chưa rõ |
| 6 | Phiên bản MySQL khai báo **cố định** (`new MySqlServerVersion(...)`) hay `ServerVersion.AutoDetect(...)`? | `InfrastructureServiceCollectionExtensions.cs` | Cố định 5.7.x. Nếu là AutoDetect thì lệnh sinh migration script ở GĐ 9 sẽ cần kết nối DB, phải đổi cách làm. |
| 7 | Phiên bản EF Core / Pomelo đang dùng (để cài `dotnet-ef` cùng phiên bản) | `ProductionMES.Infrastructure.csproj` | `8.0.2` |
| 8 | Tất cả controller đều có prefix `/api/...`? SignalR hub đúng là `/hubs/scan`? Có endpoint nào khác ngoài `/api/*`, `/hubs/*`, `/health` cần đi qua API không (vd file tĩnh upload, `/uploads`)? | Controllers, `Program.cs` (`MapHub`, `UseStaticFiles`) | Chỉ 3 nhóm trên |
| 9 | Đường dẫn project để chạy EF: Infrastructure (chứa DbContext + Migrations) và Api (startup) | Cấu trúc thư mục `src/` | `src/ProductionMES.Infrastructure`, `src/ProductionMES.Api` |
| 10 | web-admin: tên biến môi trường cho URL API là gì? Code có tự thêm `/api` vào sau không? | `web-admin/src` (chỗ tạo axios/fetch client), `web-admin/.env*` | `VITE_API_BASE_URL=https://mes.internal` |
| 11 | Station.Wpf: tên key cấu hình URL | `Station.Wpf/appsettings.json` | `ApiBaseUrl`, `SignalRHubUrl` |

---

## 4. Kết quả cần báo lại

- [ ] Đã merge thay đổi `UseForwardedHeaders` (gửi tên nhánh/commit hash).
- [ ] Đã trả lời 11 mục ở bảng trên.

Các giai đoạn sau cũng sẽ cần máy dev: build API (GĐ 5), build web-admin (GĐ 6), sinh file migration SQL
(GĐ 9). Hướng dẫn cho các giai đoạn đó sẽ gửi riêng sau khi xong giai đoạn này.
