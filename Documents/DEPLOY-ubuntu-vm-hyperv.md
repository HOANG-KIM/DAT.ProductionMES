# Hướng dẫn deploy ProductionMES lên VM Ubuntu (Hyper-V trên Windows 10 Pro)

> Tài liệu này dùng cho cả người vận hành lẫn Claude Code đọc lại về sau. Khi bắt đầu một phiên làm việc mới
> liên quan đến deploy, hãy đọc mục **0. Trạng thái hiện tại** trước để biết đã làm tới đâu, và **cập nhật lại mục
> đó** mỗi khi hoàn thành hoặc thay đổi một bước.

---

## 0. Trạng thái hiện tại

Cập nhật lần cuối: 2026-10-12 — đã chốt thông số mạng/MySQL thật (mục 2) và thứ tự triển khai: **MySQL + migration
làm sau cùng** (Giai đoạn 9). **Chưa triển khai bước nào**.

| GĐ | Hạng mục | Trạng thái | Ghi chú |
|---|---|---|---|
| 1 | Bật Hyper-V + tạo VM | ✅ Xong (2026-10-12) | VM `mes-server` đã tạo + đang chạy, boot từ ISO 24.04.5 |
| 2 | Cài Ubuntu Server 24.04 | ✅ Xong (2026-10-13) | 24.04.5, IP `192.168.1.248`, `/` = 56G, SSH + internet OK |
| 3 | Cấu hình cơ bản VM (ufw, .NET, Caddy) | ✅ Xong (2026-10-13) | ASP.NET Core 8.0.31, Caddy 2.6.2, user `mes` uid 996 |
| 4 | **Sửa code: `UseForwardedHeaders` trong `Program.cs`** | ✅ Xong (2026-10-13) | Commit `fbfc692`, merge vào `master` local (`5e8b9ed`); `master` đã gồm cả nhánh ESP32 (`c583987`). Build OK, test 424 + 160 pass. Chưa push GitHub (không chặn deploy vì máy dev tự build) |
| 5 | Deploy API (copy file, secret, systemd — **chưa start**) | 🔵 Chờ máy dev | Đã gửi `GIAI-DOAN-5-may-dev.md` (2026-10-13). API chỉ start được sau GĐ 9 |
| 6 | Deploy web-admin | ⬜ Chưa làm | Cần tạo `web-admin/.env.production` |
| 7 | Cấu hình Caddy | ⬜ Chưa làm | |
| 8 | Cài root CA + hosts trên client, đổi URL Station.Wpf | ⬜ Chưa làm | Kiểm tra tạm: thấy trang đăng nhập là đạt |
| 9 | MySQL (máy `192.168.1.107`): tạo DB/user, mở kết nối, migration, **start API** | ⬜ Chưa làm | Database `ProductionMES` chưa tồn tại — tạo mới |
| 10 | Kiểm tra toàn bộ + đổi mật khẩu admin mặc định | ⬜ Chưa làm | |

Ký hiệu: ⬜ Chưa làm · 🔵 Đang làm · ✅ Xong

Các giả định về code trong tài liệu này đã được máy dev xác nhận (2026-10-13, trên `master` sau khi merge ESP32):
connection string không có tham số phụ, tên key cấu hình, Serilog sink `File` ở index 1, `/health` có kiểm tra
MySQL, MySQL version khai báo cố định `5.7.16` (không AutoDetect), EF Core/Pomelo `8.0.2`, mọi API dưới
`/api/v1/...`, hub `/hubs/scan`, `VITE_API_BASE_URL` không kèm `/api`.

---

## 1. Bối cảnh và các quyết định đã chốt

### 1.1 Vì sao dùng VM Ubuntu thay vì deploy thẳng trên Windows 10 Pro
- Máy chủ hiện là **Windows 10 Pro** (bản client, không phải Windows Server).
- IIS trên Windows 10 bị **giới hạn khoảng 10 request đồng thời**. Mỗi trạm `Station.Wpf` giữ 1 kết nối SignalR
  (`/hubs/scan`) mở liên tục nên sẽ chạm giới hạn này rất nhanh → **không host API qua IIS trên Win10**.
- Quyết định: tạo **1 VM Ubuntu Server trên Hyper-V** (Win10 Pro có sẵn Hyper-V), gom **cả API lẫn web-admin**
  vào VM này. Không tách web-admin ra VM riêng.
- **MySQL 5.7.16 nằm trên một máy riêng (`192.168.1.107`)**, không phải máy Windows chạy Hyper-V. VM kết nối
  sang qua LAN. Máy MySQL này còn chứa database của các hệ thống khác → chỉ cấp quyền trên `ProductionMES.*`.

### 1.2 Vì sao dùng Caddy (không dùng nginx)
- Cả hai đều miễn phí, chỉ cần chọn 1. Caddy được chọn vì cấu hình ngắn, **tự tạo + tự gia hạn chứng chỉ HTTPS**
  (`tls internal`), hỗ trợ WebSocket cho SignalR sẵn.
- Với nginx phải tự tạo CA (mkcert), tự ký và tự gia hạn chứng chỉ.
- Phía client thì 2 lựa chọn như nhau: chỉ cần cài 1 file root CA.

### 1.3 Vì sao bắt buộc HTTPS dù chỉ chạy nội bộ
- Theo ADR-003, cookie đăng nhập (`access_token`, `refresh_token`, `XSRF-TOKEN`) đặt `Secure` + `SameSite=Strict`
  (`AuthController.cs`, `Program.cs` phần Antiforgery). Chạy HTTP thường thì trình duyệt không gửi cookie → không đăng nhập được.
- **Không nới lỏng `Secure`/`SameSite`**, vì ADR-003 dự tính sau này mở hệ thống ra internet.
- web-admin và API phải **cùng origin** (cùng domain, cùng HTTPS) → Caddy phục vụ cả 2 dưới 1 domain.

### 1.4 Vì sao dùng tên `mes.internal` (không dùng `mes.local`)
- Đuôi `.local` hay xung đột với mDNS trên Windows. Đuôi `.internal` là đuôi dành riêng cho mạng nội bộ.

### 1.5 Vì sao không dùng Let's Encrypt
- Website chỉ chạy nội bộ, Let's Encrypt không truy cập được domain từ internet để xác thực → dùng CA riêng của
  Caddy (`tls internal`), đổi lại client phải cài root CA.
- Khi public ra internet sau này: xem mục 15.

### 1.6 Vì sao tạo schema bằng file SQL thay vì `dotnet ef database update` trực tiếp
- User `mes` chỉ được phép đăng nhập từ IP của VM (`192.168.1.248`), máy dev không dùng được user này.
- Không muốn mở tài khoản `root` cho đăng nhập từ xa trên máy MySQL đang chứa dữ liệu hệ thống khác.
- File SQL sinh ra đọc lại được trước khi chạy → chắc chắn chỉ tác động lên database `ProductionMES`.

### 1.7 Vì sao làm MySQL + migration sau cùng
- Quyết định của người vận hành (2026-10-12): dựng xong VM, API, web-admin, Caddy, client trước; MySQL làm cuối.
- Hệ quả: API cần DB ngay khi khởi động (tự seed admin + permission), nên ở GĐ 5 chỉ cài đặt chứ **chưa start**
  API. Đến GĐ 9 mới start. Trước đó web-admin vẫn mở được, nhưng mọi lệnh gọi API trả lỗi 502 — là bình thường.

---

## 2. Thông số môi trường

Các giá trị dưới đây là **giá trị thật** đã chốt (2026-10-12), dùng xuyên suốt tài liệu.

| Thông số | Giá trị |
|---|---|
| IP máy MySQL (máy riêng, **không** phải máy chạy Hyper-V) | `192.168.1.107` |
| MySQL | 5.7.16, cổng `3306`, database `ProductionMES` |
| IP VM Ubuntu (tĩnh, nằm ngoài dải DHCP) | `192.168.1.248` |
| Gateway / DNS | `192.168.1.1` |
| Subnet | `192.168.1.0/24` |
| Domain nội bộ | `mes.internal` |
| Tên VM trong Hyper-V | `mes-server` |
| Máy chạy Hyper-V | `DESKTOP-V6OE2JL`, IP `192.168.1.98`, RAM 16 GB, chỉ có ổ C: |
| Tên card mạng vật lý của máy Hyper-V | `Ethernet` (Realtek PCIe GbE) — `ExternalSwitch` đã gắn vào card này |
| User đăng nhập Ubuntu | `mesadmin` |
| User hệ thống chạy API | `mes` |
| User MySQL cho API | `mes`@`192.168.1.248` |
| Cổng nội bộ Kestrel | `127.0.0.1:5000` |

**Không ghi mật khẩu thật vào file này** (file nằm trong git).

### 2.1 Sơ đồ

```
Máy văn phòng (trình duyệt) ─┐
                             ├─ HTTPS ─▶ VM Ubuntu 192.168.1.248 (mes.internal)  [chạy trên Hyper-V của máy Win10 Pro]
Máy trạm Station.Wpf ────────┘           ├─ Caddy :443 (tls internal)
                                         │   ├─ /api/*, /hubs/*, /health ─▶ Kestrel 127.0.0.1:5000 (systemd)
                                         │   └─ còn lại ─▶ /var/www/web-admin (file tĩnh, SPA fallback index.html)
                                         └─ API ─ TCP 3306 ─▶ MySQL 5.7.16 trên máy riêng 192.168.1.107
```

### 2.2 Vị trí file trên VM

| Đường dẫn | Nội dung | Owner |
|---|---|---|
| `/opt/productionmes/api/` | Code API đã publish (bị ghi đè mỗi lần deploy) | root |
| `/var/www/web-admin/` | `dist/` của web-admin (bị ghi đè mỗi lần deploy) | root |
| `/etc/productionmes/api.env` | Secret: connection string, JWT key… (chmod 600) | root |
| `/etc/systemd/system/productionmes-api.service` | Service chạy API | root |
| `/etc/caddy/Caddyfile` | Cấu hình Caddy | root |
| `/var/lib/productionmes/PackingTemplates/` | Template tem in US-24 (dữ liệu, **không** bị ghi đè khi deploy) | mes |
| `/var/log/productionmes/` | Log Serilog | mes |
| `/var/lib/caddy/.local/share/caddy/pki/authorities/local/root.crt` | Root CA để cài lên client | caddy |

---

## 3. Giai đoạn 1 — Bật Hyper-V và tạo VM (trên máy Windows 10 Pro)

> ⚠️ Làm **trực tiếp tại máy**, không qua Remote Desktop: bước 1.2 làm máy mất mạng vài giây khi tạo switch.

**1.1 Bật Hyper-V** — PowerShell quyền Admin, chạy xong khởi động lại máy. Nếu báo lỗi, vào BIOS bật
Virtualization (VT-x / AMD-V).
```powershell
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All
```

**1.2 Tạo External Switch** — để VM có IP cùng mạng LAN với các máy trạm:
```powershell
Get-NetAdapter                      # xem tên card mạng, vd "Ethernet"
New-VMSwitch -Name "ExternalSwitch" -NetAdapterName "Ethernet" -AllowManagementOS $true
```

**1.3 Tải Ubuntu Server 24.04 LTS** (ISO) tại releases.ubuntu.com/24.04, lưu vào `C:\Iso\`. Kiểm tra SHA256
với file `SHA256SUMS` cùng thư mục. **Dùng 24.04, không dùng 26.04** (đã chốt 2026-10-12: 24.04 có sẵn
`aspnetcore-runtime-8.0` trong kho apt).

**1.4 Tạo VM** (máy chỉ có ổ C:):
```powershell
New-VM -Name "mes-server" -Generation 2 -MemoryStartupBytes 4GB `
  -NewVHDPath "C:\VMs\mes-server.vhdx" -NewVHDSizeBytes 60GB -SwitchName "ExternalSwitch"
Set-VMProcessor mes-server -Count 2
Set-VMMemory mes-server -DynamicMemoryEnabled $false
# Bắt buộc với Ubuntu, nếu không VM sẽ không boot được (lỗi Secure Boot)
Set-VMFirmware mes-server -SecureBootTemplate MicrosoftUEFICertificateAuthority
Add-VMDvdDrive mes-server -Path "C:\Iso\ubuntu-24.04.5-live-server-amd64.iso"
Set-VMFirmware mes-server -FirstBootDevice (Get-VMDvdDrive mes-server)
# VM tự bật khi máy host khởi động lại (vd sau Windows Update)
Set-VM mes-server -AutomaticStartAction Start -AutomaticStartDelay 30 -AutomaticStopAction ShutDown
Start-VM mes-server
```
Mở **Hyper-V Manager** → double-click `mes-server` để mở console.

---

## 4. Giai đoạn 2 — Cài Ubuntu Server

Hầu hết chọn mặc định, chỉ lưu ý:
- **Network:** chọn card mạng → *Edit IPv4* → **Manual**:
  - Subnet `192.168.1.0/24`
  - Address `192.168.1.248`
  - Gateway `192.168.1.1`
  - Name servers `192.168.1.1, 8.8.8.8`
- **Storage:** *Use an entire disk* (giữ LVM). Mặc định LVM chỉ dùng ~nửa ổ → ở màn *Storage configuration*,
  *Edit* `ubuntu-lv`, đặt Size = max.
- **Profile:** tạo user `mesadmin`, server name `mes-server`.
- **SSH:** ✅ tích **Install OpenSSH server**.
- **Featured snaps:** không chọn gì.

Cài xong chọn *Reboot Now*. **Không tháo ISO trước khi bấm Reboot Now** — trình cài vẫn đang dùng `/cdrom`, tháo
sớm sẽ bị treo lúc tắt máy (`Failed unmounting cdrom.mount`, `Failed to execute shutdown binary`); gặp lỗi này thì
chỉ cần `Stop-VM mes-server -TurnOff -Force; Start-VM mes-server`, bản cài không bị ảnh hưởng. Cách đúng: bấm
Reboot Now, đợi dòng *"Please remove the installation medium"* rồi mới tháo ISO và đổi boot sang ổ cứng:
```powershell
Get-VMDvdDrive mes-server | Set-VMDvdDrive -Path $null
Set-VMFirmware mes-server -FirstBootDevice (Get-VMHardDiskDrive mes-server)
```
Sau đó nhấn Enter trong console. Từ đây SSH từ máy Windows (Windows 10 có sẵn `ssh`):
```powershell
ssh mesadmin@192.168.1.248
```

---

## 5. Giai đoạn 3 — Cấu hình cơ bản trên VM

```bash
sudo apt update && sudo apt upgrade -y
sudo timedatectl set-timezone Asia/Ho_Chi_Minh

# Firewall: chỉ mở SSH, HTTP, HTTPS
sudo ufw allow OpenSSH
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable

# .NET 8 runtime (có sẵn trong kho Ubuntu 24.04) + font cho ClosedXML + Caddy
sudo apt install -y aspnetcore-runtime-8.0 fonts-dejavu-core caddy
dotnet --list-runtimes     # phải thấy Microsoft.AspNetCore.App 8.0.x

# User riêng chạy API (không đăng nhập được) + thư mục
sudo useradd --system --no-create-home --shell /usr/sbin/nologin mes
sudo mkdir -p /opt/productionmes/api /var/www/web-admin /etc/productionmes
sudo mkdir -p /var/lib/productionmes/PackingTemplates /var/log/productionmes
sudo chown -R mes:mes /var/lib/productionmes /var/log/productionmes
```

- `fonts-dejavu-core`: ClosedXML (xuất Excel) cần font trên Linux, thiếu có thể lỗi khi tự căn độ rộng cột.
- Đã kiểm tra (2026-10-10): backend không có thư viện chỉ chạy trên Windows. QR dùng `PngByteQRCode`
  (`PackingLabelGenerator.cs`) chạy được trên Linux.

---

## 6. Giai đoạn 4 — Sửa code bắt buộc: `UseForwardedHeaders`

**Vì sao bắt buộc:** Caddy kết thúc HTTPS rồi chuyển tiếp vào Kestrel bằng HTTP thường. Antiforgery đang đặt
`Cookie.SecurePolicy = CookieSecurePolicy.Always` (`src/ProductionMES.Api/Program.cs`, phần `AddAntiforgery`),
nên nếu API không biết request gốc là HTTPS thì **mọi request POST/PUT sẽ lỗi** (antiforgery báo request không
phải SSL). Cách sửa: cho API đọc header `X-Forwarded-Proto` mà Caddy gửi kèm.

```csharp
// Program.cs — thêm cùng chỗ đăng ký service
using Microsoft.AspNetCore.HttpOverrides;

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Caddy (reverse proxy cùng VM, gọi vào qua loopback, đã nằm trong KnownProxies mặc định) kết thúc HTTPS
    // rồi chuyển tiếp HTTP vào Kestrel — phải đọc X-Forwarded-Proto để API biết request gốc là HTTPS.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// ngay sau `var app = builder.Build();` — phải đứng TRƯỚC mọi middleware khác
app.UseForwardedHeaders();
```

Trạng thái: **chưa áp dụng vào code** (xem mục 0). Khi áp dụng, nên làm trên nhánh riêng, không trộn vào nhánh
của user story khác.

---

## 7. Giai đoạn 5 — Build và cài đặt API (chưa start)

> API cần MySQL ngay khi khởi động, nên giai đoạn này chỉ copy file, tạo secret và service. **Start API ở bước 9.6.**

**5.1** Trên máy dev: làm theo `GIAI-DOAN-5-may-dev.md`. Kết quả là file `productionmes-api-<hash>.tar.gz`
(publish Release, `-r linux-x64 --self-contained false`, đã bỏ `appsettings.Development.json`). Máy dev không
cần truy cập VM.

**5.2** Copy gói lên VM rồi giải nén vào thư mục code. Trên máy host (PowerShell):
```powershell
scp C:\duong\dan\productionmes-api-<hash>.tar.gz mesadmin@192.168.1.248:/tmp/
```
Trên VM:
```bash
rm -rf /tmp/api && mkdir /tmp/api
tar -xzf /tmp/productionmes-api-<hash>.tar.gz -C /tmp/api
ls -l /tmp/api/ProductionMES.Api.dll /tmp/api/appsettings*.json   # phải có dll + đúng 1 file appsettings.json
sudo rsync -a --delete --chown=root:root /tmp/api/ /opt/productionmes/api/
```
`--chown=root:root`: code thuộc root, user `mes` chỉ đọc được, không sửa được (an toàn hơn).

**5.3** Tạo file secret, **chỉ root đọc được**:
```bash
openssl rand -base64 48        # copy kết quả làm Jwt__Key
sudo nano /etc/productionmes/api.env
```
Nội dung (ghi đè các giá trị placeholder trong `appsettings.json`):
```
ConnectionStrings__DefaultConnection=Server=192.168.1.107;Port=3306;Database=ProductionMES;User=mes;Password=MatKhauManh_DoiCaiNay;
Jwt__Key=<chuỗi vừa sinh ở trên>
Cors__WebAdminOrigin=https://mes.internal
PackingTemplateStorage__RelativeFolderPath=/var/lib/productionmes/PackingTemplates
Serilog__WriteTo__1__Args__path=/var/log/productionmes/log-.txt
```
```bash
sudo chmod 600 /etc/productionmes/api.env
```

Giải thích:
- Mật khẩu ở đây phải trùng với mật khẩu sẽ đặt cho user `mes` ở bước 9.1. Có thể chọn mật khẩu ngay bây giờ,
  ghi vào đây, và dùng lại ở bước 9.1. **Chỉ dùng chữ, số và `-_.@`** (dài ≥ 16 ký tự): dấu `;` làm hỏng connection
  string, dấu `'` làm hỏng lệnh SQL ở 9.1, còn `"`, `\`, `$`, `%` dễ bị systemd hiểu sai trong file env.
- Connection string: đã xác nhận code không dùng tham số phụ nào (`SslMode`, `CharSet`…). `Port=3306` ghi rõ cho dễ
  đọc, bỏ đi cũng được vì 3306 là mặc định.
- `PackingTemplateStorage__RelativeFolderPath` dùng đường dẫn tuyệt đối — `Program.cs` hỗ trợ sẵn
  (`Path.IsPathRooted`). Mục đích: template tem in không nằm trong thư mục code, không bị `rsync --delete` xoá.
- `Serilog__WriteTo__1__...` trỏ tới phần tử thứ 2 (index 1, sink `File`) trong mảng `Serilog:WriteTo` của
  `appsettings.json`. Nếu sau này đổi thứ tự sink trong `appsettings.json` thì phải sửa index này. **Bắt buộc**
  ghi đè: mặc định path là `Logs/log-.txt` (tương đối) → ghi vào `/opt/productionmes/api/Logs`, thư mục của root,
  user `mes` không ghi được và còn bị `rsync --delete` xoá mỗi lần deploy.

**5.4** Tạo service systemd — `sudo nano /etc/systemd/system/productionmes-api.service`:
```ini
[Unit]
Description=ProductionMES API
After=network-online.target
Wants=network-online.target

[Service]
User=mes
WorkingDirectory=/opt/productionmes/api
ExecStart=/usr/bin/dotnet /opt/productionmes/api/ProductionMES.Api.dll
Restart=always
RestartSec=5
KillSignal=SIGINT
SyslogIdentifier=productionmes-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000
Environment=DOTNET_NOLOGO=1
EnvironmentFile=/etc/productionmes/api.env

[Install]
WantedBy=multi-user.target
```
```bash
sudo systemctl daemon-reload
# CHƯA enable/start ở đây — làm ở bước 9.6 sau khi có database
```

`ASPNETCORE_ENVIRONMENT=Production` → Swagger tự tắt (chỉ bật ở Development).

---

## 8. Giai đoạn 6 — Build và deploy web-admin

Trên máy dev, tạo `web-admin/.env.production`:
```
VITE_API_BASE_URL=https://mes.internal
```
```powershell
cd web-admin
npm ci
npm run build
scp -r dist mesadmin@192.168.1.248:/tmp/
```
Trên VM:
```bash
sudo rsync -a --delete /tmp/dist/ /var/www/web-admin/
```

---

## 9. Giai đoạn 7 — Cấu hình Caddy

`sudo nano /etc/caddy/Caddyfile`, xoá nội dung cũ, thay bằng:
```
mes.internal {
    tls internal
    encode gzip

    handle /api/*  { reverse_proxy 127.0.0.1:5000 }
    handle /hubs/* { reverse_proxy 127.0.0.1:5000 }
    handle /health { reverse_proxy 127.0.0.1:5000 }

    handle {
        root * /var/www/web-admin
        try_files {path} /index.html
        file_server
    }
}
```
```bash
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
# Lấy root CA để cài lên client
sudo cp /var/lib/caddy/.local/share/caddy/pki/authorities/local/root.crt /tmp/mes-root.crt
sudo chmod 644 /tmp/mes-root.crt
```

- `try_files {path} /index.html`: web-admin là SPA, mọi route không phải file thật đều trả `index.html` để
  React Router xử lý (F5 ở trang con không bị 404).
- Root CA của Caddy có hạn khoảng 10 năm. Chứng chỉ của website Caddy tự gia hạn, không ảnh hưởng client.
- Nếu Caddy log cảnh báo không cài được root vào trust store của chính VM: bỏ qua, không ảnh hưởng.

---

## 10. Giai đoạn 8 — Cấu hình máy client

Client gồm: máy văn phòng dùng trình duyệt mở web-admin **và** các máy trạm `Station.Wpf`.
**Không phải cài phần mềm nào**, chỉ cài 1 file chứng chỉ và khai báo tên miền.

**8.1** Copy root CA về máy dev:
```powershell
scp mesadmin@192.168.1.248:/tmp/mes-root.crt .
```

**8.2** Trên **từng máy client** (PowerShell quyền Admin):
```powershell
certutil -addstore -f Root mes-root.crt
Add-Content C:\Windows\System32\drivers\etc\hosts "`n192.168.1.248  mes.internal"
```
- Chrome, Edge và `HttpClient` của Station.Wpf dùng chung store chứng chỉ của Windows → cài 1 lần là đủ.
- Firefox dùng store riêng: bật `security.enterprise_roots.enabled` trong `about:config` hoặc import thủ công.
- Nếu router/DNS nội bộ tạo được bản ghi `mes.internal → 192.168.1.248` thì bỏ dòng sửa file hosts.
- Nếu máy trong Active Directory: đẩy root CA bằng Group Policy thay vì cài tay từng máy.

**8.3** Máy trạm WPF — sửa `appsettings.json` của `Station.Wpf`:
```json
"ApiBaseUrl": "https://mes.internal/",
"SignalRHubUrl": "https://mes.internal/hubs/scan",
```
(`StationApiKeyValue` vẫn đọc từ `appsettings.local.json` như hiện tại.)

Máy trạm có cắm thiết bị ESP32 (máy thông điện): sửa thêm `Tester:PortName` (vd `COM5`) trong `appsettings.json`
của trạm. ESP32 nói chuyện với Station.Wpf qua cổng COM (USB, 115200 baud), **không kết nối mạng** → không cần
cài root CA hay cấu hình gì cho ESP32.

**8.4 Kiểm tra tạm (API chưa chạy):**
- Mở `https://mes.internal` → có ổ khoá, không cảnh báo chứng chỉ, **hiện trang đăng nhập** → đạt.
- Đăng nhập sẽ **báo lỗi** (Caddy trả 502 vì API chưa start) → bình thường, sẽ hết sau GĐ 9.
- Station.Wpf chưa kết nối được → bình thường.

---

## 11. Giai đoạn 9 — MySQL (máy `192.168.1.107`), migration và start API

> Các bước 9.1–9.3 làm **trên máy MySQL `192.168.1.107`**, không phải máy chạy Hyper-V.

**9.1** Mở MySQL Workbench hoặc `mysql` CLI bằng tài khoản root **ngay trên máy MySQL**, tạo database + user
riêng cho VM. User này chỉ có quyền trên `ProductionMES`, không đụng tới database của hệ thống khác.
Mật khẩu phải **trùng với mật khẩu đã ghi trong `api.env`** ở bước 5.3:
```sql
CREATE DATABASE IF NOT EXISTS ProductionMES CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
CREATE USER 'mes'@'192.168.1.248' IDENTIFIED BY 'MatKhauManh_DoiCaiNay';
GRANT ALL PRIVILEGES ON ProductionMES.* TO 'mes'@'192.168.1.248';
FLUSH PRIVILEGES;
```

**9.2 Kiểm tra MySQL có nhận kết nối từ mạng LAN không.** Trên máy MySQL, mở CMD:
```
netstat -ano | findstr :3306
```
- Thấy `0.0.0.0:3306` → MySQL đã nhận kết nối từ mọi IP, **không cần sửa gì**. Bộ cài MySQL 5.7 trên Windows
  mặc định không ghi `bind-address` nên thường rơi vào trường hợp này.
- Chỉ thấy `127.0.0.1:3306` → mở `C:\ProgramData\MySQL\MySQL Server 5.7\my.ini`, sửa `bind-address` thành
  `0.0.0.0` (hoặc xoá dòng đó), rồi restart service MySQL (`services.msc`, thường tên `MySQL57`).
  ⚠️ Restart làm gián đoạn các hệ thống khác đang dùng MySQL này, nên chọn giờ ít người dùng.

**9.3** Mở firewall Windows trên máy MySQL cho **riêng IP của VM** (PowerShell quyền Admin):
```powershell
New-NetFirewallRule -DisplayName "MySQL from MES VM" -Direction Inbound -Protocol TCP `
  -LocalPort 3306 -RemoteAddress 192.168.1.248 -Action Allow
```
(Nếu các hệ thống khác đã kết nối được vào MySQL này qua LAN thì có thể đã có rule mở 3306 sẵn. Vẫn tạo rule
trên cho rõ ràng, không ảnh hưởng gì.)

**9.4** Kiểm tra từ VM: `nc -zv 192.168.1.107 3306` → phải thấy `succeeded`.

**9.5 Tạo/cập nhật schema.** API **không tự chạy migration** khi khởi động (chỉ tự seed tài khoản admin và
permission). Lý do dùng file SQL: xem mục 1.6.

a) Trên máy dev, tại thư mục repo, sinh file SQL (lệnh này **không** kết nối database):
```powershell
dotnet tool install --global dotnet-ef --version 8.0.2   # nếu chưa có
dotnet ef migrations script --idempotent -p src/ProductionMES.Infrastructure -s src/ProductionMES.Api `
  -o migrate.sql
```
`--idempotent`: file kiểm tra bảng `__EFMigrationsHistory`, migration nào đã chạy thì bỏ qua → lần deploy sau
chạy lại cùng cách này được.

b) Mở `migrate.sql` xem lướt: không được có tên database nào khác ngoài `ProductionMES`. Có thể tìm nhanh:
```powershell
Select-String -Path migrate.sql -Pattern "DROP DATABASE|CREATE DATABASE|USE "
```
→ không có kết quả nào là đúng. EF Core chỉ tạo bảng trong database đang `USE`, không đụng tới database khác.

Kiểm tra file có đủ migration mới nhất, gồm 3 migration của nhánh ESP32 (2026-10-12):
```powershell
Select-String -Path migrate.sql -Pattern "AddTesterModeSwitchAndScanTesterMode|AddTesterEventLog|AddScanTesterResultColumns" | Select-Object -First 3
```
→ phải thấy cả 3 tên.

c) Copy `migrate.sql` sang máy MySQL, chạy bằng root (Workbench: *File → Open SQL Script*, hoặc CLI):
```sql
USE ProductionMES;
SOURCE C:/duong/dan/migrate.sql;
```
⚠️ Bắt buộc `USE ProductionMES;` trước, nếu không bảng sẽ được tạo nhầm vào database đang chọn.

**9.6 Start API** (trên VM). Lần start đầu API tự seed tài khoản admin và permission:
```bash
sudo systemctl enable --now productionmes-api
sudo systemctl status productionmes-api              # phải thấy "active (running)"
sudo journalctl -u productionmes-api -n 50 | grep "Now listening"   # phải là http://127.0.0.1:5000
curl http://127.0.0.1:5000/health                    # phải thấy "Healthy" (health check có kiểm tra MySQL)
sudo journalctl -u productionmes-api -f              # xem log nếu có lỗi
```
- `/health` kiểm tra cả kết nối MySQL → `Unhealthy` nghĩa là API chạy nhưng không vào được DB (xem mục 14).
- `Program.cs` có `UseHttpsRedirection()`: gọi thẳng `http://127.0.0.1:5000` trên VM không bị redirect vì Kestrel
  không có cổng HTTPS (log có thể cảnh báo *"Failed to determine the https port for redirect"* — bỏ qua được).
  Request qua Caddy có `X-Forwarded-Proto: https` nên cũng không bị redirect.

---

## 12. Giai đoạn 10 — Kiểm tra toàn bộ và bảo mật lần đầu

1. Mở `https://mes.internal` → có ổ khoá, không cảnh báo chứng chỉ.
2. Đăng nhập `admin` / `Admin@123` (tài khoản seed sẵn, US-22) → **đổi mật khẩu ngay**.
3. Thử thêm/sửa dữ liệu (POST/PUT) → không lỗi CSRF.
4. Mở 1 máy trạm WPF → thử scan, kiểm tra Andon board cập nhật realtime (SignalR).
5. Trên máy chạy Hyper-V, tạo checkpoint VM:
   ```powershell
   Checkpoint-VM mes-server -SnapshotName "deploy-lan-dau"
   ```
6. Trên máy MySQL, backup database lần đầu (xem lệnh `mysqldump` ở mục 13).

---

## 13. Quy trình deploy các lần sau

1. **Nếu có migration mới:**
   - Backup database trước (trên máy MySQL). Checkpoint VM **không** chứa dữ liệu, vì DB nằm ở máy khác:
     ```
     mysqldump -u root -p --single-transaction --routines ProductionMES > ProductionMES_YYYYMMDD.sql
     ```
   - Chạy lại bước 9.5 **trước** khi copy API mới.
2. (Khuyến nghị) Tạo checkpoint VM: `Checkpoint-VM mes-server -SnapshotName "truoc-deploy-YYYYMMDD"`.
3. **API:** chạy lại bước 5.1 trên máy dev, copy + giải nén gói vào `/tmp/api` như bước 5.2, rồi trên VM:
   ```bash
   sudo systemctl stop productionmes-api
   sudo rsync -a --delete --chown=root:root /tmp/api/ /opt/productionmes/api/
   sudo systemctl start productionmes-api
   curl http://127.0.0.1:5000/health
   ```
4. **web-admin:** chạy lại Giai đoạn 6. Không cần restart gì.

---

## 14. Xử lý sự cố thường gặp

| Triệu chứng | Nguyên nhân hay gặp | Cách xử lý |
|---|---|---|
| VM không boot, báo lỗi Secure Boot | Chưa đổi Secure Boot template | `Set-VMFirmware mes-server -SecureBootTemplate MicrosoftUEFICertificateAuthority` |
| Trước GĐ 9: web-admin mở được nhưng đăng nhập lỗi / 502 | API chưa start (chưa có DB) | Bình thường — làm GĐ 9 |
| POST/PUT lỗi 400 hoặc 500 liên quan antiforgery/CSRF | Thiếu `UseForwardedHeaders` (mục 6), hoặc truy cập bằng IP thay vì `mes.internal` | Áp dụng mục 6; luôn truy cập qua domain |
| Đăng nhập xong vẫn bị đẩy ra / gọi API bị 401 | Web-admin và API khác origin, cookie `SameSite=Strict` không được gửi | Kiểm tra `VITE_API_BASE_URL` và `Cors__WebAdminOrigin` đều là `https://mes.internal` |
| Trình duyệt cảnh báo chứng chỉ | Chưa cài root CA, hoặc cài nhầm store "Personal" | Cài lại vào **Trusted Root Certification Authorities** của **Local Machine** |
| Station.Wpf báo lỗi SSL / không kết nối | Máy trạm chưa cài root CA hoặc chưa có dòng hosts | Làm lại bước 8.2 trên máy trạm |
| Andon board không cập nhật realtime | Sai `SignalRHubUrl`, hoặc Caddy thiếu `handle /hubs/*` | Kiểm tra bước 8.3 và Caddyfile |
| API không start, log báo không kết nối MySQL | Sai mật khẩu / chưa mở firewall 3306 / `bind-address` trên máy `192.168.1.107` | Kiểm tra lại bước 9.1–9.4 |
| Log báo `Access denied for user 'mes'@'...'` | Mật khẩu trong `api.env` khác bước 9.1, hoặc user MySQL tạo với IP khác IP thật của VM | So mật khẩu; kiểm tra IP VM (`ip a`) có đúng `192.168.1.248` không |
| Log báo `Table ... doesn't exist` | Chưa chạy migration, hoặc chạy nhầm database | Làm lại bước 9.5, nhớ `USE ProductionMES;` |
| Upload template tem in lỗi quyền ghi | Thư mục template không thuộc user `mes` | `sudo chown -R mes:mes /var/lib/productionmes` |
| Xuất Excel lỗi font | Thiếu font trên Linux | `sudo apt install -y fonts-dejavu-core` rồi restart API |
| Sau khi host khởi động lại, hệ thống không chạy | VM không tự bật | `Set-VM mes-server -AutomaticStartAction Start` |

Xem log:
- API: `sudo journalctl -u productionmes-api -n 200` hoặc `/var/log/productionmes/`
- Caddy: `sudo journalctl -u caddy -n 200`

---

## 15. Lộ trình khi muốn public ra internet sau này

**Cân nhắc trước:** nếu chỉ nhân viên công ty cần truy cập từ xa, dùng **VPN** (WireGuard, Tailscale, VPN của
router) an toàn hơn nhiều so với public — giữ nguyên toàn bộ cấu hình trong tài liệu này.

Nếu public thật:
1. **Domain + HTTPS:** tạo `mes.congty.vn` trỏ về IP public; bỏ dòng `tls internal` trong Caddyfile → Caddy tự
   lấy chứng chỉ Let's Encrypt miễn phí, client không cần cài root CA nữa. Đổi `VITE_API_BASE_URL` và
   `Cors__WebAdminOrigin` sang domain mới.
2. **Mạng:** chỉ mở 80/443 về VM (nên đặt VM trong DMZ). **Không bao giờ mở MySQL ra ngoài.** Station.Wpf vẫn
   đi nội bộ; nên chặn `/hubs/scan` và các endpoint `StationApiKey` với IP ngoài LAN.
3. **Bảo mật bổ sung:** rate limit đăng nhập, khoá tài khoản sau nhiều lần sai, HSTS + security header (CSP,
   X-Frame-Options), fail2ban, `unattended-upgrades`. Cân nhắc Cloudflare (Free) hoặc Cloudflare Tunnel để
   không phải mở cổng.
4. **Hạ tầng:** MySQL 5.7 đã hết hỗ trợ từ 10/2023 → nâng lên 8.0 trước khi public (phải sửa
   `MySqlServerVersion` trong `InfrastructureServiceCollectionExtensions.cs` và test lại). Cân nhắc chuyển khỏi
   Windows 10 Pro sang Windows Server / máy chủ Linux / VPS. Thiết lập backup DB tự động ra ngoài máy chủ.
