# ProductionMES: Giai đoạn 6, việc cần làm trên máy dev (build web-admin)

> Gửi cho: người phụ trách mã nguồn ProductionMES (máy dev có repo).
> Ngày gửi: 2026-10-13.
> Tài liệu gốc: `DEPLOY-ubuntu-vm-hyperv.md` (mục 8, Giai đoạn 6).

---

## 1. Mục tiêu

Build web-admin (Vite + React) cho production, trỏ API về `https://mes.internal`. Đóng gói thư mục `dist`
thành **1 file `.tar.gz`** rồi gửi cho người deploy, giống cách đã làm ở Giai đoạn 5.

Trên server, Caddy phục vụ web-admin và API **cùng domain** `https://mes.internal`:
- `/api/*`, `/hubs/*`, `/health` → API
- mọi đường dẫn còn lại → file tĩnh của web-admin, dùng SPA fallback về `index.html`

Vì vậy web-admin phải được build với **`base` là `/`** (chạy ở gốc domain), và URL API là
`https://mes.internal`, **không có `/api` ở cuối**. Câu trả lời #10 ở Giai đoạn 4 đã xác nhận: code tự ghi
`/api/v1/...` trong từng lời gọi.

---

## 2. Các bước thực hiện

### 2.1 Lấy đúng code
Build từ **cùng commit với API** đã deploy ở Giai đoạn 5 (`30ca8ce`). Nếu `master` đã có commit mới, báo người
deploy trước khi build, vì web-admin và API nên cùng một phiên bản.
```powershell
git checkout master
git pull
git status                       # phải sạch
git log --oneline -1             # mong đợi: 30ca8ce
$hash = git rev-parse --short HEAD
```

### 2.2 Tạo file `web-admin/.env.production`
```powershell
cd web-admin
Set-Content -Path .env.production -Value "VITE_API_BASE_URL=https://mes.internal" -Encoding ascii
Get-Content .env.production
```
File này không chứa secret. Có thể commit vào repo để lần build sau dùng lại. Nếu commit thì ghi lại hash mới.

### 2.3 Kiểm tra không có cấu hình nào ghi đè
Vite ưu tiên các nguồn sau **cao hơn** `.env.production`. Cả hai phải **không tồn tại hoặc rỗng**:
```powershell
Test-Path .env.production.local          # phải False
$env:VITE_API_BASE_URL                   # phải rỗng (không in ra gì)
```
(`.env` và `.env.local` có ưu tiên **thấp hơn** `.env.production` nên không ảnh hưởng, ví dụ giá trị
`https://localhost:7230` của bản dev.)

Kiểm tra `base` trong cấu hình Vite:
```powershell
Select-String -Path vite.config.* -Pattern "base"
```
→ không có dòng nào, hoặc có `base: '/'`. Nếu là giá trị khác, **dừng lại và báo người deploy**.

### 2.4 Build
```powershell
Remove-Item -Recurse -Force dist -ErrorAction SilentlyContinue
npm ci
npm run build
```
Build phải kết thúc không lỗi. Có cảnh báo về kích thước chunk thì bỏ qua được.

### 2.5 Kiểm tra bản build
```powershell
Test-Path dist/index.html                                                   # phải True
(Select-String -Path dist/assets/*.js -Pattern "https://mes.internal" -List).Count   # phải >= 1
(Select-String -Path dist/assets/*.js -Pattern "localhost:7230" -List).Count         # phải 0
Get-ChildItem dist -Recurse -Filter *.map | Measure-Object | Select-Object Count
```
- Nếu dòng `localhost:7230` khác 0: bản build vẫn trỏ về máy dev. Xem lại bước 2.3.
- Nếu có file `.map` (source map): xoá đi, vì chúng làm lộ mã nguồn và không cần trên server:
  ```powershell
  Get-ChildItem dist -Recurse -Filter *.map | Remove-Item
  ```

### 2.6 Đóng gói
```powershell
tar -czf "productionmes-web-admin-$hash.tar.gz" -C dist .
Get-Item "productionmes-web-admin-$hash.tar.gz" | Select-Object Name, Length
(Get-FileHash "productionmes-web-admin-$hash.tar.gz" -Algorithm SHA256).Hash
(tar -tzf "productionmes-web-admin-$hash.tar.gz" | Measure-Object).Count
```

### 2.7 Gửi cho người deploy
Gửi file `productionmes-web-admin-<hash>.tar.gz` qua USB, thư mục chia sẻ hoặc kênh nội bộ như Giai đoạn 5.

---

## 3. Kết quả cần báo lại

- [ ] Commit đã build (`git log --oneline -1`), có trùng `30ca8ce` không. Nếu commit `.env.production` thì ghi hash mới.
- [ ] Kết quả bước 2.3: `.env.production.local` có tồn tại không, biến môi trường có rỗng không, giá trị `base`
- [ ] Kết quả 4 dòng kiểm tra ở bước 2.5, và đã xoá `.map` chưa (nếu có)
- [ ] Tên file `.tar.gz`, kích thước, SHA256, số mục trong gói (bước 2.6)

Giai đoạn sau còn cần máy dev: sinh file migration SQL (GĐ 9). Hướng dẫn sẽ gửi riêng.
