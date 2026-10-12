# US-14: Kết nối, heartbeat & phục hồi cổng COM với máy thông điện

> **Viết lại 12/10/2026** theo `Documents/spec-firmware-app-link.md` (mục 3–4) và `Documents/spec-app-wpf-station.md` (mục 5, 7.3). Mở rộng từ "kết nối & phục hồi COM" sang kết nối + heartbeat + đồng bộ lại + log. Thay thế vai trò của US-13 (đã hủy). Tên thư mục giữ nguyên.

**Là** Công nhân vận hành trạm / Hệ thống
**Tôi muốn** ứng dụng trạm tự mở cổng COM tới máy thông điện, theo dõi kết nối bằng heartbeat, tự kết nối lại và đồng bộ trạng thái khi nối lại
**Để** không bao giờ lưu kết quả dựa trên một đường truyền không đáng tin, không mất/nhầm kết quả khi cáp bị rút hay máy bị reset, và có dấu vết để truy vết sự cố phần cứng

**Điều kiện áp dụng:** chỉ khi trạm `UseArduino = true` **và** công tắc "Đang sử dụng máy thông điện" = BẬT (US-11). Ngược lại App đóng cổng COM và không PING.

## Cấu hình cục bộ (không có màn hình Cài đặt trong app)
Section `Tester` trong `Station.Wpf/appsettings.json` (cục bộ tại từng trạm, Options pattern): `PortName` (vd `COM5`), `BaudRate = 115200`, `PingIntervalMs = 1000`, `LinkTimeoutMs = 5000`, `CommandTimeoutMs = 2000`, `ReconnectIntervalMs = 2000`. Không có vai trò "Kỹ thuật"; sửa file khi triển khai/bảo trì. Các trường `ComPort`/`BaudRate`/`CommandProtocol` trên `WorkStation` phía server **không còn dùng** cho kết nối.

**Acceptance Criteria**
- **AC1 — Tự mở cổng khi khởi động, không làm máy reset**
  - Given trạm có máy, công tắc BẬT
  - When ứng dụng khởi động
  - Then mở cổng theo section `Tester` với 115200, 8N1, `Handshake.None`, **`DtrEnable = false`, `RtsEnable = false`** (DTR/RTS nối mạch auto-reset của ESP32); máy không bị reset; App gửi `#HELLO?`, nhận `#HELLO` và hiển thị FW, GM (và ghi PINS, BOOT)
- **AC2 — Không mở cổng khi không dùng máy**
  - Given trạm `UseArduino = false`, hoặc công tắc TẮT
  - When ứng dụng chạy
  - Then không mở/đóng cổng COM, không PING (máy tự chạy độc lập sau ≤ 5 giây)
- **AC3 — Heartbeat và phát hiện mất kết nối; chặn quét**
  - Given cổng đang mở
  - When App gửi `#PING` mỗi 1000 ms; không nhận `#PONG` trong 5000 ms
  - Then App coi là **mất kết nối** (`Disconnected`): hiển thị banner đỏ nhấp nháy **"MẤT KẾT NỐI MÁY THÔNG ĐIỆN"** trên `AndonBoardWindow`, **chặn quét** (mã quét không được xử lý, không gửi `BIND`, không lưu gì — vì không tin cậy được kết quả), ghi event log `LINK_LOST`
- **AC4 — Tự kết nối lại**
  - Given cổng bị rút / lỗi I/O / mất PONG
  - When hệ thống phát hiện
  - Then đóng cổng và thử mở lại mỗi 2000 ms, không cần khởi động lại App; khi nối lại ghi event log `LINK_RESTORED`, gửi `#HELLO?` và đồng bộ theo AC5
- **AC5 — Đồng bộ lại sau khi kết nối có lại** *(spec-app 7.3)*
  - Given kết nối vừa phục hồi
  - When nhận `#HELLO`, `BOOT` khác lần trước (máy đã reset)
  - Then kết quả đang chờ (nếu có) coi như mất: ghi event log `VOID_REBOOT`, về `Ready`
  - And khi nhận `#PONG` đầu tiên, so `ST`/`SEQ` với kết quả App đang giữ (`s`):
    - `ST=WAIT_BIND,SEQ=s`, App có giữ → tiếp tục `WaitScan`
    - `ST=BINDING,SEQ=s`, App đã nhận `BIND_OK` → tiếp tục `Saving`
    - `ST=WAIT_BIND,SEQ=k` với k ≠ s, hoặc App không giữ gì → gửi `#RESULT?`, nhận lại kết quả → `WaitScan`
    - `SEQ=0`, App có giữ kết quả → máy đã hủy (mất link quá lâu): ghi event log `VOID_LINK_LOST`, về `Ready`
    - `SEQ=0`, App không giữ gì → `Ready`
- **AC6 — Mất kết nối khi đang có kết quả chờ**
  - Given App ở `WaitScan`/`Binding`/`Saving`
  - When mất kết nối máy
  - Then chuyển `Disconnected`, giữ kết quả trong bộ nhớ đánh dấu "chưa chắc", chờ đồng bộ ở AC5; nếu `Saving` đã commit server thì bản ghi vẫn hợp lệ (xem US-12/AC16)
- **AC7 — Xử lý mọi trạng thái `ST` và mã lỗi `ERR` của firmware**
  - Given App nhận `PONG` hoặc `#ERR`
  - When `ST` thuộc `IDLE`/`TESTING`/`WAIT_BIND`/`BINDING`/`WAIT_REMOVE`/`BROWSE`/`NO_SAMPLE`; hoặc `ERR,CODE` thuộc `BAD_CMD`/`BAD_SEQ`/`NO_PENDING`/`TOO_LONG`/`BUSY`
  - Then App không lỗi/không treo: chỉ các `ST` liên quan (`WAIT_BIND`, `BINDING`) tham gia đồng bộ; `BROWSE`/`WAIT_REMOVE`/`IDLE`/`TESTING` giữ nguyên trạng thái App; `ERR` được ghi log, không đổi trạng thái; riêng `BUSY` xử lý theo US-12/AC10
- **AC8 — Banner "Máy chưa có mẫu"**
  - Given `HELLO` có `GM=0000` hoặc `PONG` có `ST=NO_SAMPLE`
  - When App nhận
  - Then hiển thị banner **"Máy chưa có mẫu — cần Learn"**; không chặn cổng (việc Learn thực hiện trên máy)
  - And `GM` của `RESULT` khác `GM` lần trước → ghi log "Mẫu (golden matrix) đã thay đổi"
- **AC9 — Chỉ báo kết nối trên `AndonBoardWindow`**
  - Given trạm có máy, công tắc BẬT
  - When App chạy
  - Then hiển thị chỉ báo "Máy: KẾT NỐI / MẤT KẾT NỐI" cùng FW và GM (4 ký tự hex)
- **AC10 — Parser chịu lỗi**
  - Given nhận dữ liệu từ cổng
  - When dòng không bắt đầu bằng `#` (log debug firmware, rác bootloader); dòng thiếu key bắt buộc; key lạ; dòng > 256 byte chưa có `\n`; `\r\n`
  - Then dòng không phải `#` chỉ ghi file, không xử lý; key lạ bị bỏ qua; dòng hỏng/quá dài ghi log và bỏ, không làm sập App; tra key theo tên, không theo vị trí
- **AC11 — Log serial ra file, không vào DB**
  - Given App giao tiếp Serial
  - When có dòng gửi/nhận
  - Then ghi **file log Serilog mức Debug** (tách theo ngày); **không** ghi vào DB, **không** có bảng `LichSuLenhArduino` (PING mỗi giây sinh quá nhiều dòng)
- **AC12 — Event log sự kiện máy**
  - Given các sự kiện `VOID_REMOVED`, `VOID_DISCARD`, `VOID_REBOOT`, `VOID_LINK_LOST`, `LINK_LOST`, `LINK_RESTORED`, `BIND_FAIL`, `SCAN_ON_NG`, `DB_ERROR`
  - When xảy ra
  - Then ghi bản ghi event log lên server (trạm, thời điểm, loại, mã tem/SEQ nếu có, chi tiết, người duyệt nếu có); nếu ghi lỗi (mất server) thì chỉ ghi file Serilog, **không chặn thao tác** của người vận hành
- **AC13 — Đọc/ghi không chặn UI, một lệnh tại một thời điểm**
  - Given App gửi `BIND`/`SAVED`/`DISCARD`/`RESULT?`/`HELLO?`
  - When chờ phản hồi
  - Then đọc cổng ở luồng nền (không chặn UI thread); mỗi lệnh chờ phản hồi tương ứng hoặc tối đa 2000 ms mới gửi lệnh kế; `PING` gửi độc lập theo timer; ghi có khóa để PING và lệnh khác không chen nhau

## Ngoài phạm vi / để sau
- Kiểm tra `PINS` (từ `HELLO`) so với Model đang chọn và chặn quét khi sai máy/firmware: **để sau**. Hiện chỉ đọc `PINS` để hiển thị/ghi log.

**Nguồn FR:** FR-14 (viết lại 12/10/2026)
**Phụ thuộc:** US-11 (điều kiện BẬT/TẮT), US-12 (state machine dùng kết nối này), US-07 (`AndonBoardWindow`); ADR-006/ADR-007 cho giao diện. Thay thế US-13 (đã hủy). Không còn phụ thuộc cấu hình COM phía server của US-04.
**Cờ cảnh báo mục 8.2:** Không trực tiếp.

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần (code + test xong, chưa nghiệm thu với máy thật / COM thật)
- **Cập nhật:** 2026-10-12

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-12 (ba, viết lại theo spec máy thông điện ESP32)
- Mở rộng story: DTR/RTS = false, heartbeat PING 1s / timeout 5s thay timeout 45s (US-13 hủy), tự kết nối lại mỗi 2s, chặn quét khi mất kết nối, đồng bộ lại theo `HELLO`/`BOOT`/`PONG` (spec-app 7.3), xử lý đủ `ST`/`ERR`, banner thiếu mẫu, event log.
- **Bỏ** bảng `LichSuLenhArduino`; log serial vào/ra ghi file Serilog mức Debug.
- Cấu hình COM/baud/timeout chuyển về `appsettings.json` cục bộ (section `Tester`); không có màn hình Cài đặt.
- Chưa có code triển khai.

#### 2026-10-12 (dev, triển khai kết nối COM / heartbeat / đồng bộ lại / event log) — commit `3b4713b` (thư viện + event log backend), `90df2ad` (đồng bộ lại — state machine), `102cb15` (Station.Wpf + công cụ giả lập)
- **Thư viện mới `src/ProductionMES.Station.Tester`** (net8.0, KHÔNG phụ thuộc WPF/backend; package `System.IO.Ports 8.0.0`): `TesterOptions` (section `Tester`: `PortName`, `BaudRate=115200`, `PingIntervalMs=1000`, `LinkTimeoutMs=5000`, `CommandTimeoutMs=2000`, `ReconnectIntervalMs=2000`), `ProtocolParser` + `LineAssembler` (AC10: tra key theo tên, bỏ key lạ, thiếu key bắt buộc => dòng hỏng, dòng không `#` chỉ log, `\r\n`, dòng > 256 byte bỏ rồi đồng bộ lại ở `\n` kế tiếp), `ISerialPort`/`SystemSerialPort` (115200 8N1, `Handshake.None`, `DtrEnable = false`, `RtsEnable = false` đặt TRƯỚC khi mở — AC1), `TesterLink` (luồng đọc nền; `#HELLO?` ngay khi mở; `#PING` mỗi 1000 ms độc lập; mất PONG > 5000 ms / lỗi I/O => đóng cổng + raise `PortClosed`, thử mở lại mỗi 2000 ms — AC3/AC4; ghi có `lock` — AC13; `Stop()` đóng cổng + ngừng PING + không tự mở lại — AC2; log RX/TX/dòng rác/dòng hỏng qua `ITesterLog`).
- **Đồng bộ lại (AC5–AC8)** nằm trong `TesterStationController` (xem US-12): HELLO `BOOT` khác lần trước => `VOID_REBOOT`; PONG đầu tiên sau (tái) kết nối đối chiếu `ST`/`SEQ` đúng bảng spec-app 7.3 (`WAIT_BIND`+SEQ khớp => `WaitScan`; `BINDING`+SEQ khớp => `Saving`; `WAIT_BIND`/`BINDING` với SEQ khác hoặc App không giữ => `#RESULT?`; `SEQ=0` + App giữ => `VOID_LINK_LOST`; `SEQ=0` không giữ => `Ready`); mọi `ST` (`IDLE/TESTING/WAIT_BIND/BINDING/WAIT_REMOVE/BROWSE/NO_SAMPLE`) và mọi `ERR` (`BAD_CMD/BAD_SEQ/NO_PENDING/TOO_LONG/BUSY`) được xử lý không treo; `GM=0000`/`ST=NO_SAMPLE` => banner "Máy chưa có mẫu — cần Learn" (không chặn cổng); `GM` của `RESULT` đổi => ghi log "Mẫu (golden matrix) đã thay đổi".
- **Event log (AC12):** entity `TesterEventLog` + migration `AddTesterEventLog` + `POST api/v1/tester/events` (StationApiKey, 204). Loại sự kiện (enum `TesterEventType`, lưu chuỗi PascalCase): `VoidRemoved`, `VoidDiscard`, `VoidReboot`, `VoidLinkLost`, `LinkLost`, `LinkRestored`, `BindFail`, `ScanOnNg`, `DbError` (tương ứng `VOID_REMOVED`... trong tài liệu). Station.Wpf ghi lỗi (mất server) chỉ ra file Serilog, không chặn thao tác.
- **Station.Wpf:** DI `TesterLink`/`TesterStationController`; Serilog ghi `logs/station-YYYYMMDD.log` mức Debug (toàn bộ dòng serial vào/ra — AC11, không ghi DB, không có bảng `LichSuLenhArduino`); `AndonBoardWindow`: chỉ báo "Máy: KẾT NỐI · FW · GM · số pin" (AC9), banner đỏ nhấp nháy "MẤT KẾT NỐI MÁY THÔNG ĐIỆN" + chặn quét (AC3), banner "Máy chưa có mẫu — cần Learn" (AC8). Cổng COM chỉ mở khi trạm `UseArduino` + công tắc BẬT (AC2).
- **Công cụ giả lập firmware:** `tools/ProductionMES.Tester.Simulator` (console, dùng `System.IO.Ports`, nối cặp COM ảo com0com — hướng dẫn trong comment `Program.cs`; phím `i p f r m b g s q`) + lớp `MachineSimulator` (logic thuần theo spec mục 4–5) dùng lại trong test tích hợp.
- **Test:** `ProductionMES.Station.Tester.Tests` (xUnit, 160 test, pass): `ProtocolParserTests`, `LineAssemblerTests`, `TesterLinkTests` (cổng COM giả: mở cổng + HELLO?/PING, mất PONG, rút cáp, tự mở lại, Stop/Start, handler ném exception), `TesterStationControllerTests` (đồng bộ lại / ERR / ST), `TesterIntegrationTests` (link + controller + `MachineSimulator`: S1, S7, S7b, S8...); `TesterStationServiceTests.RecordEventAsync_*`.
- **Build:** `dotnet build ProductionMES.sln` 0 lỗi 0 cảnh báo.
- **Quyết định tự chọn:**
  - "Kết nối" của App = đã mở cổng VÀ đã nhận PONG đầu tiên + đồng bộ xong (`Disconnected` chặn quét cho tới lúc đó); `RESULT`/`VOID` nhận được khi chưa đồng bộ xong bị bỏ qua/không mở khóa quét — PONG đầu tiên + `#RESULT?` sẽ lấy lại trạng thái đúng.
  - Cổng mở mà không có PONG nào trong `LinkTimeoutMs` kể từ lúc mở => đóng và thử lại (cổng nhầm thiết bị).
  - `LINK_LOST`/`LINK_RESTORED` chỉ ghi sau khi đã từng kết nối được (lần mở cổng thất bại lúc khởi động không sinh event).
  - Thời điểm event log lấy theo giờ server; người duyệt của `VOID_DISCARD` do Station.Wpf gửi lên (endpoint dùng StationApiKey, không có Bearer).
  - Chưa kiểm tra `PINS` theo Model (đúng phạm vi: chỉ hiển thị/log).
  - Lưu ý phần cứng: một số driver USB-UART vẫn kéo DTR trong thoáng chốc lúc mở handle (trước khi áp DCB) làm ESP32 reset; firmware đã chịu được (gửi HELLO lúc boot, App đồng bộ theo `BOOT`).
- **Còn thiếu / lý do giữ 🟡:** chưa chạy với máy thật / cặp COM ảo (đã kiểm tra bằng cổng giả + `MachineSimulator` ở mức giao thức); chưa đo "không chậm đáng kể" của luồng PING trên PC trạm; chưa chạy 1 ca dài (S12) để xác nhận không mất kết nối.
