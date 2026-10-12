# Đặc tả App WPF — Trạm thông điện + quét QR

> App chạy trên PC tại trạm, kết nối máy thông điện ESP32 qua cổng COM và đầu quét QR, lưu kết quả theo SN.
> **Giao thức serial: mục 4 của `spec-firmware-app-link.md` là nguồn chân lý.** File này chỉ tóm tắt lại phần App cần dùng; nếu hai file lệch nhau thì sửa theo file firmware.

---

## 1. Mục tiêu

- Gắn kết quả thông điện **OK** của máy với SN đọc từ mã QR trên dây, lưu vào CSDL để truy vết. Dây NG không đi qua App (tháo ra sửa, sửa xong thông điện lại).
- Quy trình: **thông điện trước, quét QR sau** — dây đã đặt sẵn trên bàn thông điện.
- Vẫn làm việc được khi máy thông điện hỏng: chế độ "Không dùng máy thông điện" chỉ quét QR rồi lưu, kết quả ghi là `BYPASS`.

## 2. Quyết định đã chốt

| # | Quyết định |
|---|---|
| A1 | Công tắc **"Sử dụng máy thông điện"** (BẬT/TẮT) trong App |
| A2 | BẬT: chỉ lưu khi có kết quả từ máy + QR. Mất kết nối máy → **chặn quét**, không lưu được gì |
| A3 | TẮT: App đóng cổng COM, quét QR là lưu ngay với kết quả **`BYPASS`** (không bao giờ ghi là `OK`) |
| A4 | Đổi công tắc cần **quyền Tổ trưởng/Kỹ thuật** + chọn lý do; mọi lần đổi được ghi log |
| A5 | **Chỉ dây OK mới quét QR và lưu.** Dây NG: máy tự báo NG và cho tháo dây ngay, máy không chủ động gửi gì — dây đem đi sửa, sửa xong quay lại thông điện như sợi mới. Nếu người vận hành vẫn cố quét QR dây NG: App gửi `BIND`, máy trả `REASON=NG`, App hiện thông báo lỗi và không lưu (7.5) |
| A6 | Huỷ kết quả đang chờ (không lưu) cần quyền Tổ trưởng, được ghi log |
| A7 | Heartbeat: App gửi `#PING` mỗi 1000 ms; không nhận `#PONG` trong 5000 ms → coi là mất kết nối |
| A8 | **Mã QR / SN là việc riêng của App**: đọc, kiểm tra định dạng (nhiều định dạng), xử lý trùng mã theo logic sẵn có của App. SN **không** gửi xuống máy — máy chỉ biết `SEQ` |
| A9 | Đầu quét QR chế độ **HID (bàn phím)** |
| A10 | CSDL **MySQL** |
| A11 | Máy chỉ gửi kết quả từ thông điện tự động / nút Start. Nút Test trên máy chỉ để chẩn đoán, không gửi gì. Learn/Lưu trên máy không cần App cho phép và không gửi gì cho App |

## 3. Công nghệ

| Thành phần | Lựa chọn |
|---|---|
| Nền tảng | .NET 8, WPF, Windows 10/11 |
| Kiến trúc UI | MVVM (`CommunityToolkit.Mvvm`) |
| Serial | `System.IO.Ports` (NuGet) |
| CSDL | MySQL (`MySqlConnector`, hoặc EF Core + `Pomelo.EntityFrameworkCore.MySql`), đi qua 1 lớp repository |
| Cấu hình | `appsettings.json` cạnh file exe |
| Log | Serilog, ghi file theo ngày (gồm toàn bộ dòng serial vào/ra ở mức Debug) |

## 4. Cấu trúc project đề xuất

```
HarnessStation/
├── HarnessStation.App/            # WPF: View, ViewModel
│   ├── Views/MainWindow.xaml
│   ├── ViewModels/MainViewModel.cs
│   └── Views/Dialogs/ (đăng nhập tổ trưởng, chọn lý do, cài đặt)
├── HarnessStation.Core/           # Không phụ thuộc WPF — test được bằng unit test
│   ├── Tester/
│   │   ├── TesterLink.cs          # Cổng COM, đọc dòng, PING, gửi lệnh
│   │   ├── ProtocolParser.cs      # Dòng "#..." -> object; bỏ qua dòng khác
│   │   └── Messages.cs            # HelloMsg, PongMsg, ResultMsg, BindOkMsg, ...
│   ├── Scanner/
│   │   ├── IScanner.cs
│   │   └── HidScanner.cs          # Đầu quét chế độ bàn phím (HID)
│   ├── Station/StationController.cs  # State machine mục 7
│   └── Data/ (repository, model)
└── HarnessStation.Tests/          # Unit test parser + state machine (máy giả lập)
```

`StationController` là nơi duy nhất ra quyết định; ViewModel chỉ hiển thị trạng thái và chuyển thao tác người dùng vào controller.

## 5. Kết nối máy thông điện

### 5.1 Cấu hình cổng

| Thuộc tính | Giá trị | Ghi chú |
|---|---|---|
| `PortName` | Từ cài đặt (VD `COM5`) | Cổng USB ghi "UART" trên DevKitC-1 |
| `BaudRate` | 115200 | 8N1, `Handshake.None` |
| `DtrEnable` | **false** | DTR/RTS nối vào mạch auto-reset của ESP32 — bật lên là máy reset |
| `RtsEnable` | **false** | như trên |
| `NewLine` | `"\n"` | Khi nhận: cắt `\r` cuối dòng nếu có |
| `Encoding` | ASCII | |
| `ReadTimeout` | không dùng | Đọc bằng luồng nền, không dùng `ReadLine()` chặn trên UI thread |

### 5.2 Đọc / ghi

- **Đọc**: 1 luồng nền (hoặc `Task` dài hạn) đọc byte → ghép dòng theo `\n` → dòng bắt đầu `#` đưa vào `ProtocolParser`; dòng khác là log của firmware (debug, rác lúc reset) → chỉ ghi file log, **không xử lý**.
- Dòng dài > 256 byte mà chưa gặp `\n` → bỏ bộ đệm (dòng hỏng).
- Kết quả parse đẩy về `StationController` qua một hàng đợi / `Dispatcher` — mọi thay đổi trạng thái xảy ra trên một luồng duy nhất.
- **Ghi**: mỗi lệnh 1 lần `Write(line + "\n")`, có `lock` để PING và lệnh khác không chen nhau.
- **Một lệnh tại một thời điểm**: sau khi gửi `BIND`/`SAVED`/`DISCARD`/`RESULT?`/`HELLO?` phải chờ phản hồi tương ứng (hoặc timeout 2000 ms) mới gửi lệnh kế. `PING` không tính — gửi độc lập theo timer.

### 5.3 Heartbeat và trạng thái kết nối

- Timer 1000 ms gửi `#PING` (chỉ khi công tắc BẬT và cổng đang mở).
- `lastPongAt` cập nhật mỗi khi nhận `#PONG`. Quá 5000 ms không có PONG → **Mất kết nối**.
- Cổng bị rút / lỗi I/O → đóng cổng, thử mở lại mỗi 2000 ms.
- Khi kết nối có lại: gửi `#HELLO?`, đối chiếu `BOOT`, sau đó đồng bộ theo `PONG` (mục 7.3).

### 5.4 Tóm tắt giao thức App dùng

App → máy: `#PING` · `#BIND,SEQ=n` · `#SAVED,SEQ=n` · `#DISCARD,SEQ=n` · `#RESULT?` · `#HELLO?`

Máy → App: `#HELLO` · `#PONG,ST=..,SEQ=..` · `#RESULT,SEQ=n,R=OK,GM=..` (chỉ khi OK) · `#BIND_OK` · `#BIND_FAIL,REASON=..` · `#UNLOCKED` · `#VOID,REASON=..` · `#ERR,CODE=..`

Parser phải: tra key theo tên (không theo vị trí), bỏ qua key lạ (để firmware thêm trường sau này không làm App lỗi), coi dòng thiếu key bắt buộc là dòng hỏng (ghi log, không xử lý).

Máy **không** gửi gì khi NG và khi Learn/Lưu — App không cần xử lý các trường hợp này.

## 6. Đầu quét QR (HID)

### 6.1 Nhận mã quét

- Đầu quét cấu hình hậu tố **Enter** (CR).
- Bắt phím ở cấp cửa sổ (`PreviewTextInput` + `PreviewKeyDown` của `MainWindow`), không dùng TextBox — để mã quét không phụ thuộc ô nào đang được chọn.
- Phân biệt đầu quét với người gõ tay: khoảng cách giữa các ký tự < `HidMaxInterCharMs` (mặc định 50 ms) và kết thúc bằng Enter; chuỗi gõ chậm bị bỏ.
- Nếu App có ô nhập liệu khác (tìm SN, đăng nhập…), chỉ bắt mã quét khi các ô đó không có focus, hoặc tạm ngừng bắt quét khi hộp thoại đang mở.

### 6.2 Hạn chế của HID và cách giảm thiểu

| Hạn chế | Giảm thiểu |
|---|---|
| Chỉ nhận khi cửa sổ App đang focus — quét lúc App mất focus thì mã gõ vào cửa sổ khác | Banner đỏ "APP KHÔNG ĐƯỢC CHỌN — KHÔNG NHẬN MÃ QUÉT" khi `Deactivated`; cân nhắc chạy toàn màn hình/kiosk |
| Bộ gõ tiếng Việt (Unikey/EVKey) có thể biến đổi ký tự quét | Hướng dẫn tắt bộ gõ trên PC trạm, hoặc đầu quét gửi dạng keycode thường; kiểm tra với các mã QR thực tế |
| Bố cục bàn phím khác US làm sai ký tự đặc biệt | Đặt bố cục bàn phím US cho PC trạm |

### 6.3 Kiểm tra mã

Định dạng mã QR và xử lý trùng mã **theo logic sẵn có của App** (A8) — ngoài phạm vi đặc tả này. Spec này chỉ yêu cầu: mã không hợp lệ → báo lỗi, **không gửi BIND**, không đổi trạng thái.

## 7. State machine của App (`StationController`)

### 7.1 Chế độ BẬT (có máy thông điện)

| Trạng thái | Màn hình | Sự kiện → trạng thái kế |
|---|---|---|
| `Disconnected` | Banner đỏ "MẤT KẾT NỐI MÁY THÔNG ĐIỆN" | Có PONG → `Ready` (sau khi đồng bộ, mục 7.3) |
| `Ready` | "Đặt dây và chờ thông điện" | `RESULT` → `WaitScan` · quét mã hợp lệ → gửi `#BIND,SEQ=0`, hiển thị theo phản hồi (bảng 7.5), ở lại `Ready` |
| `WaitScan` | Chữ lớn OK (xanh), "QUÉT MÃ QR TRÊN DÂY" | Quét mã hợp lệ (qua kiểm tra định dạng + trùng mã của App, 6.3) → gửi BIND → `Binding` · `VOID` → báo lý do, ghi log → `Ready` · Tổ trưởng bấm Huỷ → gửi DISCARD → `Discarding` |
| `Binding` | "Đang xác nhận…" | `BIND_OK` → ghi DB → `Saving` · `BIND_FAIL REMOVED` → báo "Dây đã bị rút, đo lại", ghi log → `Ready` · `BIND_FAIL` khác → báo lỗi → `WaitScan` · timeout 2 s → gửi lại BIND tối đa 2 lần, sau đó → `WaitScan` + báo lỗi |
| `Saving` | "Đang lưu…" | Ghi DB thành công → gửi SAVED → chờ `UNLOCKED` → báo "ĐÃ LƯU <SN>" → `Ready` · Ghi DB lỗi → báo lỗi, ở lại `Saving`, nút "Thử lại" / Tổ trưởng "Huỷ" (DISCARD) |
| `Discarding` | — | `VOID REASON=DISCARD` → ghi log → `Ready` |

Mất kết nối ở bất kỳ trạng thái nào → `Disconnected`. Nếu lúc đó đang `WaitScan`/`Binding`/`Saving` thì giữ lại kết quả đang chờ trong bộ nhớ, đánh dấu "chưa chắc", chờ đồng bộ (7.3).

**Thứ tự ghi DB và SAVED**: chỉ gửi `SAVED` sau khi DB đã commit. Nếu ghi DB xong mà mất kết nối trước khi `UNLOCKED`: bản ghi vẫn hợp lệ (dây đã được xác nhận bằng `BIND_OK`); máy sẽ tự mở khoá khi mất link quá 5 s.

### 7.2 Chế độ TẮT (không dùng máy)

- Cổng COM máy thông điện đóng, không PING (máy tự về chạy độc lập sau ≤ 5 s).
- Banner vàng thường trực: "ĐANG CHẠY KHÔNG CÓ MÁY THÔNG ĐIỆN — lý do: …".
- Quét mã hợp lệ (6.3) → lưu bản ghi `Result = BYPASS` → báo "ĐÃ LƯU (BYPASS) <SN>".

### 7.3 Đồng bộ lại sau khi kết nối có lại

1. Nhận `#HELLO`: nếu `BOOT` khác lần trước → máy đã reset → kết quả đang chờ (nếu có) bị mất: ghi log `VOID_REBOOT`, về `Ready`.
2. Nhận `#PONG` đầu tiên, so `ST`/`SEQ` với trạng thái App:

| PONG | App đang giữ kết quả SEQ = s | Xử lý |
|---|---|---|
| `ST=WAIT_BIND,SEQ=s` | có | Tiếp tục `WaitScan` |
| `ST=BINDING,SEQ=s` | có, đã nhận BIND_OK | Tiếp tục `Saving` |
| `ST=WAIT_BIND,SEQ=k` (k ≠ s hoặc App không giữ gì) | — | Gửi `#RESULT?`, nhận lại kết quả → `WaitScan` |
| `SEQ=0` | có | Kết quả đã bị máy huỷ (mất link quá lâu) → ghi log `VOID_LINK_LOST` → `Ready` |
| `SEQ=0` | không | `Ready` |

### 7.4 Quy tắc nghiệp vụ

| Tình huống | Xử lý |
|---|---|
| Quét QR khi App không giữ kết quả OK nào (chế độ BẬT) | Vẫn gửi `#BIND,SEQ=0` để máy trả lời đúng lý do — xem 7.5. Không tự kết luận "Chưa thông điện" |
| Mã trùng / sai định dạng | Theo logic sẵn có của App (A8). Yêu cầu duy nhất từ phía máy: chỉ gửi `BIND` khi mã đã được chấp nhận; từ chối thì giữ `WaitScan` (máy vẫn khoá, chờ quét mã khác hoặc Tổ trưởng Huỷ) |
| Kết quả bị `VOID` (rút dây trước khi lưu) | Ghi vào `EventLog`, báo "Dây đã bị rút — thông điện lại" |
| `GM` của RESULT khác `GM` lần trước | Ghi log "Mẫu (golden matrix) đã thay đổi" |
| `GM=0000` trong HELLO | Banner "Máy chưa có mẫu — cần Learn" |
| `PINS` trong HELLO khác cấu hình model đang chọn | Cảnh báo, chặn quét (sai máy / sai firmware) |

### 7.5 Phản hồi khi quét mà App không giữ kết quả OK (`#BIND,SEQ=0`)

| Phản hồi của máy | App hiển thị | Ghi log |
|---|---|---|
| `BIND_FAIL,REASON=NG` | Thông báo lỗi đỏ: **"DÂY NG"**. Giữ tới khi người vận hành bấm OK hoặc hết vài giây. Không có âm báo | `event_log` loại `SCAN_ON_NG` kèm SN vừa quét |
| `BIND_FAIL,REASON=TESTING` | "Đang thông điện — chờ kết quả rồi quét lại" | không |
| `BIND_FAIL,REASON=NO_PENDING` | "Chưa thông điện" | không |
| `BIND_FAIL,REASON=BAD_SEQ` | (máy đang có kết quả OK mà App lỡ mất) gửi `#RESULT?` → `WaitScan` → người vận hành quét lại | `BIND_FAIL` |
| Timeout 2 s | "Không phản hồi từ máy thông điện" | `BIND_FAIL` |

App không lưu bản ghi kết quả trong mọi trường hợp ở bảng này. Máy cũng không đổi trạng thái (dây NG vẫn được tháo ra bình thường).

## 8. CSDL

MySQL (InnoDB, `utf8mb4`). Các bảng dưới là phần **bổ sung cho tính năng thông điện**; nếu App đã có bảng kết quả theo SN thì thêm các cột này vào bảng sẵn có thay vì tạo bảng mới. Cột phục vụ logic trùng mã (số lần đo, lịch sử NG…) do logic sẵn có của App quyết định.

### 8.1 `test_record`

```sql
CREATE TABLE test_record (
  id               BIGINT AUTO_INCREMENT PRIMARY KEY,
  serial_number    VARCHAR(128) NOT NULL,
  result           ENUM('OK','BYPASS') NOT NULL, -- NG không đi qua App
  mode             ENUM('TESTER','BYPASS') NOT NULL,
  tester_boot_id   CHAR(8) NULL,                -- BOOT; NULL nếu BYPASS
  tester_seq       INT UNSIGNED NULL,           -- SEQ;  NULL nếu BYPASS
  golden_crc       CHAR(4) NULL,                -- GM
  firmware_version VARCHAR(16) NULL,            -- FW
  station_id       VARCHAR(32) NOT NULL,
  model_code       VARCHAR(64) NULL,
  operator         VARCHAR(64) NULL,
  tested_at        DATETIME(3) NOT NULL,        -- lúc nhận RESULT (BYPASS: lúc quét)
  saved_at         DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  KEY ix_serial (serial_number),
  KEY ix_station_time (station_id, tested_at),
  UNIQUE KEY ux_tester_result (station_id, tester_boot_id, tester_seq)
);
```

- `ux_tester_result` chống lưu trùng **một kết quả** của máy (VD App gửi lại sau khi mất mạng). MySQL cho phép nhiều dòng NULL trong UNIQUE nên các bản ghi BYPASS không bị ảnh hưởng.
- Thời gian lấy theo giờ PC trạm (máy thông điện không có đồng hồ thực). Nếu nhiều trạm dùng chung server, đồng bộ giờ PC (NTP) hoặc dùng `CURRENT_TIMESTAMP` của server.

### 8.2 `event_log`

```sql
CREATE TABLE event_log (
  id            BIGINT AUTO_INCREMENT PRIMARY KEY,
  at            DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  station_id    VARCHAR(32) NOT NULL,
  type          VARCHAR(32) NOT NULL,
  serial_number VARCHAR(128) NULL,
  tester_seq    INT UNSIGNED NULL,
  detail        JSON NULL,       -- VD {"reason":"REMOVED","gm":"3A7F"}
  operator      VARCHAR(64) NULL,
  KEY ix_station_time (station_id, at)
);
```

`type`: `VOID_REMOVED`, `VOID_DISCARD`, `VOID_REBOOT`, `VOID_LINK_LOST`, `LINK_LOST`, `LINK_RESTORED`, `BIND_FAIL`, `SCAN_ON_NG`, `DB_ERROR`.

### 8.3 `mode_change_log`

```sql
CREATE TABLE mode_change_log (
  id          BIGINT AUTO_INCREMENT PRIMARY KEY,
  at          DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  station_id  VARCHAR(32) NOT NULL,
  from_mode   ENUM('TESTER','BYPASS') NOT NULL,
  to_mode     ENUM('TESTER','BYPASS') NOT NULL,
  reason      VARCHAR(64) NOT NULL,
  approved_by VARCHAR(64) NOT NULL,
  note        VARCHAR(255) NULL
);
```

### 8.4 Mất kết nối MySQL

- Ghi DB lỗi ở `Saving` → báo lỗi, **không gửi `SAVED`**: máy vẫn khoá, dây vẫn trên jig, PING vẫn chạy nên máy không tự huỷ. Nút "Thử lại"; Tổ trưởng có thể "Huỷ" (DISCARD).
- Không dùng hàng đợi ghi offline: nếu không lưu được thì không mở khoá máy — đảm bảo mọi dây đi qua trạm đều có bản ghi.
- Các bản ghi `event_log` không ghi được lúc mất mạng → ghi vào file log Serilog (không chặn thao tác).

## 9. Giao diện

```
┌──────────────────────────────────────────────────────────────┐
│ Trạm: ST-01   Model: HNS-16A   NV: Nguyễn A     ● Máy: KẾT NỐI │
│ [Sử dụng máy thông điện: BẬT]   FW 1.4.0  Mẫu 3A7F             │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│                       ██████  OK                             │
│                                                              │
│                 >>> QUÉT MÃ QR TRÊN DÂY <<<                   │
│                                                              │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│ Ca này: OK 312  BYPASS 0            Lần lưu cuối: HNS-000123 │
│ 10 bản ghi gần nhất ...                                      │
└──────────────────────────────────────────────────────────────┘
```

- Màu nền vùng giữa: xanh (OK chờ quét), xám (Ready), đỏ đậm nhấp nháy (mất kết nối), vàng (banner BYPASS).
- Không dùng âm thanh trên PC (hiện tại không cần) — mọi thông báo chỉ bằng chữ và màu.
- Chữ lớn, đọc được từ ~1,5 m.
- Nút: "Huỷ kết quả" (yêu cầu Tổ trưởng), "Cài đặt" (yêu cầu Kỹ thuật), công tắc chế độ (yêu cầu Tổ trưởng), "Tra cứu SN".

## 10. Phân quyền

| Thao tác | Vận hành | Tổ trưởng | Kỹ thuật |
|---|---|---|---|
| Quét và lưu | ✔ | ✔ | ✔ |
| Huỷ kết quả đang chờ | | ✔ | ✔ |
| Bật/tắt máy thông điện | | ✔ | ✔ |
| Cài đặt (COM, trạm, CSDL) | | | ✔ |

Quyền liên quan tới mã trùng theo logic sẵn có của App.

Đăng nhập Tổ trưởng/Kỹ thuật dạng hộp thoại tạm thời (mật khẩu hoặc quẹt thẻ), chỉ cho thao tác đó rồi trở lại người vận hành.

## 11. Cấu hình `appsettings.json`

```json
{
  "Station": { "Id": "ST-01" },
  "Tester": {
    "PortName": "COM5",
    "BaudRate": 115200,
    "PingIntervalMs": 1000,
    "LinkTimeoutMs": 5000,
    "CommandTimeoutMs": 2000,
    "ReconnectIntervalMs": 2000
  },
  "Scanner": {
    "HidMaxInterCharMs": 50
  },
  "Database": {
    "ConnectionString": "Server=192.168.1.10;Port=3306;Database=harness;User ID=station;Password=***;"
  },
  "Models": [ { "Code": "HNS-16A", "Pins": 16 } ]
}
```

Trạng thái công tắc "Sử dụng máy thông điện" lưu trong CSDL (bảng cài đặt) — không trong file JSON — để không bị sửa tay bỏ qua log.

## 12. Kiểm thử

### 12.1 Unit test (không cần phần cứng)

- `ProtocolParser`: từng loại dòng, key đảo thứ tự, key lạ, thiếu key bắt buộc, dòng log không có `#`, `\r\n`, dòng > 256 byte, giải mã `F`.
- `StationController` với `FakeTesterLink`: chạy hết bảng 7.1, 7.3, 7.4.

### 12.2 Giả lập máy thông điện

Công cụ giả lập (console app hoặc script Python + cặp cổng COM ảo com0com) trả lời PING và cho phép bấm phím để phát `RESULT OK/NG`, `VOID`, ngừng PONG, reset (`HELLO` với BOOT mới). Dùng để phát triển App khi chưa có máy thật.

### 12.3 Kịch bản nghiệm thu với máy thật

| # | Kịch bản | Kỳ vọng |
|---|---|---|
| S1 | Mở App, chọn COM | Máy không bị reset; hiện FW, PINS, GM |
| S2 | Dây OK → quét QR | Lưu OK, máy hiện `Remove harness...` |
| S3 | Dây NG → Exit trên máy | App không đổi trạng thái; máy cho rút dây ngay |
| S3b | S3 → cố tình quét QR khi dây NG còn trên jig | Thông báo lỗi đỏ "DÂY NG", không có bản ghi kết quả, có `SCAN_ON_NG` kèm SN; máy hiện `NG - not saved` |
| S3c | S3b → rút dây → quét lại | "Chưa thông điện" |
| S3d | Quét QR trong lúc máy đang thông điện (chưa Pass) | "Đang thông điện — chờ kết quả" |
| S4 | Có kết quả → rút dây → quét QR | Từ chối; log `VOID_REMOVED` |
| S5 | Quét QR khi chưa thông điện | "Chưa thông điện" |
| S6 | Quét mã bị logic App từ chối (trùng / sai định dạng) | Không gửi BIND, máy vẫn khoá; quét mã đúng sau đó lưu bình thường |
| S7 | Rút cáp USB máy khi đang chờ quét | Banner đỏ, chặn quét; cắm lại → đồng bộ đúng theo 7.3 |
| S8 | Reset máy (nút EN) khi đang chờ quét | Log `VOID_REBOOT`, về Ready |
| S9 | Tắt công tắc (Tổ trưởng) → quét QR | Lưu `BYPASS`; máy chạy độc lập sau ≤ 5 s; có log đổi chế độ |
| S10 | Bật lại công tắc | Kết nối lại, quét QR khi chưa thông điện bị từ chối |
| S11 | Mất kết nối MySQL (rút mạng / dừng service) khi đang lưu | Báo lỗi, máy vẫn khoá; có mạng lại → "Thử lại" lưu thành công, máy mở khoá |
| S12 | Chạy 1 ca (vài trăm dây) | Không mất kết nối, không bản ghi trùng `(station, BootId, Seq)` |
| S13 | App mất focus (click sang cửa sổ khác) rồi quét | Banner cảnh báo hiện; mã không bị xử lý nhầm |
| S14 | Bấm nút Test trên máy khi đang kết nối | App không nhận kết quả nào, trạng thái App không đổi |
| S15 | Learn + Lưu trên máy khi đang kết nối | App không nhận gì, trạng thái không đổi |

## 13. Vấn đề còn mở

Không còn. Đã chốt: chỉ OK mới quét/lưu (A5), mã QR do App xử lý (A8), đầu quét HID (A9), MySQL (A10), nút Test/Learn không gửi gì (A11).

Đăng nhập người vận hành, in tem/báo cáo, cấu trúc bảng kết quả: **App đã có sẵn**, ngoài phạm vi spec này — mục 8 và 10 chỉ là gợi ý cho phần bổ sung, áp dụng theo cấu trúc sẵn có của App.
