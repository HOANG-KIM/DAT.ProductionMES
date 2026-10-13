# ProductionMES: Giai đoạn 5, việc cần làm trên máy dev (build API)

> Gửi cho: người phụ trách mã nguồn ProductionMES (máy dev có repo).
> Ngày gửi: 2026-10-13.
> Tài liệu gốc: `DEPLOY-ubuntu-vm-hyperv.md` (mục 7, Giai đoạn 5).

---

## 1. Mục tiêu

Build API ở chế độ Release cho Linux, đóng gói thành **1 file `.tar.gz`** rồi gửi cho người deploy. Người deploy
sẽ tự copy file lên VM Ubuntu, nên máy dev **không cần** kết nối hay đăng nhập vào VM.

API sẽ chạy trên VM bằng .NET runtime có sẵn (ASP.NET Core **8.0.31**). Vì vậy build ở dạng
**framework-dependent**, không đóng gói kèm runtime.

---

## 2. Các bước thực hiện

### 2.1 Lấy đúng code
Build từ `master`. Bản này đã gồm `UseForwardedHeaders` (`fbfc692`) và nhánh ESP32 (`c583987`).
```powershell
git checkout master
git status                       # phải sạch, không có thay đổi chưa commit
git log --oneline -5             # xác nhận có 5e8b9ed (merge forwarded-headers) và c583987 (merge ESP32)
$hash = git rev-parse --short HEAD
$hash
```

### 2.2 Publish
Chạy tại thư mục gốc repo:
```powershell
Remove-Item -Recurse -Force publish/api -ErrorAction SilentlyContinue
dotnet publish src/ProductionMES.Api -c Release -r linux-x64 --self-contained false -o publish/api
```
- `-r linux-x64`: chỉ lấy thư viện native cho Linux, gói nhỏ hơn.
- `--self-contained false`: dùng .NET runtime có sẵn trên VM.

### 2.3 Dọn file không được đưa lên server
```powershell
Remove-Item publish/api/appsettings.Development.json -ErrorAction SilentlyContinue
Get-ChildItem publish/api -Filter "appsettings*.json" | Select-Object Name
```
→ chỉ được còn **`appsettings.json`**. Nếu còn file khác, ví dụ `appsettings.local.json`, cũng xoá đi.

### 2.4 Kiểm tra trước khi đóng gói
```powershell
Test-Path publish/api/ProductionMES.Api.dll                      # phải True
Select-String -Path publish/api/appsettings.json -Pattern "Password=|\"Key\""
```
- Xem các dòng in ra. `Password` trong connection string và `Jwt:Key` **phải là giá trị giả** (placeholder),
  không được là mật khẩu thật. Trên server, các giá trị này sẽ bị ghi đè bằng file secret riêng.
  Nếu là giá trị thật: **dừng lại và báo người deploy**, không gửi gói đi.

Kiểm tra thêm thư mục template tem in mặc định:
```powershell
Get-ChildItem publish/api/App_Data -Recurse -ErrorAction SilentlyContinue | Select-Object FullName
```
→ ghi lại kết quả ở mục 3. Nếu có file template mẫu đi kèm code, người deploy cần copy chúng sang thư mục dữ
liệu trên server.

### 2.5 Đóng gói
Windows 10 có sẵn `tar`:
```powershell
tar -czf "productionmes-api-$hash.tar.gz" -C publish/api .
Get-Item "productionmes-api-$hash.tar.gz" | Select-Object Name, Length
(Get-FileHash "productionmes-api-$hash.tar.gz" -Algorithm SHA256).Hash
```

### 2.6 Gửi cho người deploy
Gửi file `productionmes-api-<hash>.tar.gz` qua thư mục chia sẻ, USB hoặc kênh nội bộ. **Không** đưa lên dịch vụ
công cộng.

---

## 3. Kết quả cần báo lại

- [ ] Tên file `.tar.gz` (có commit hash) và kích thước
- [ ] SHA256 của file (ở bước 2.5)
- [ ] Kết quả `git log --oneline -5` (bước 2.1)
- [ ] Xác nhận `appsettings.json` trong gói chỉ chứa giá trị giả cho mật khẩu và JWT key (bước 2.4)
- [ ] Danh sách file trong `App_Data` (bước 2.4), hoặc ghi "không có"

Các giai đoạn sau còn cần máy dev: build web-admin (GĐ 6) và sinh file migration SQL (GĐ 9). Hướng dẫn sẽ
gửi riêng.
