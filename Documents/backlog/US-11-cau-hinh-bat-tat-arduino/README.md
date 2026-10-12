# US-11: Trạm có máy thông điện & công tắc "Đang sử dụng máy thông điện" (Bypass)

> **Viết lại 12/10/2026** theo `Documents/spec-firmware-app-link.md` (giao thức) và `Documents/spec-app-wpf-station.md`. Thay thế hoàn toàn nội dung cũ (cờ `SuDungArduino` kích hoạt luồng "scan tem trước → chờ OK → timeout 45s"). Tên thư mục giữ nguyên để link ổn định; thuật ngữ "Arduino" trong tên thư mục nay chỉ máy thông điện ESP32.

**Là** Quản trị hệ thống (Admin)
**Tôi muốn** đánh dấu trạm nào có gắn máy thông điện (cờ `WorkStation.UseArduino`, đã có từ US-04)
**Để** chỉ trạm có máy mới chạy luồng "thông điện trước, quét QR sau" và mới hiện công tắc chế độ; các trạm khác không bị ảnh hưởng

**Là** Tổ trưởng (Supervisor/Admin/Manager có permission động `TesterMode.Change` — ADR-004)
**Tôi muốn** bật/tắt công tắc "Đang sử dụng máy thông điện" ngay tại trạm, bắt buộc chọn lý do
**Để** vẫn sản xuất được khi máy thông điện hỏng/đang sửa, mà mọi lần chuyển chế độ đều có dấu vết (ai duyệt, từ/đến, lý do)

## Hai cờ — phân biệt rõ
| Cờ | Ý nghĩa | Ai đổi | Lưu ở đâu |
|---|---|---|---|
| "Trạm có máy thông điện" = `WorkStation.UseArduino` | Về mặt vật lý trạm có gắn máy | Admin, màn hình quản lý trạm (US-04) | DB server (đã có) |
| "Đang sử dụng máy thông điện" | Công tắc runtime BẬT/TẮT | Tổ trưởng đăng nhập tại trạm | **DB server** (không nằm trong `appsettings.json` trạm) — để không sửa tay bỏ qua log |

**Acceptance Criteria**
- **AC1 — Trạm không có máy chạy luồng thường** *(AC-20 gốc)*
  - Given trạm có `UseArduino = false`
  - When công nhân scan tem
  - Then luồng scan y hệt hiện tại (FR-07/FR-08/FR-18/FR-27); không mở cổng COM, không PING, **không hiển thị** công tắc chế độ, không có bước chờ máy
- **AC2 — Trạm có máy + công tắc BẬT chạy luồng US-12**
  - Given trạm có `UseArduino = true` và công tắc "Đang sử dụng máy thông điện" = BẬT
  - When trạm vận hành
  - Then App kết nối máy (US-14) và vận hành theo luồng "thông điện trước → quét QR sau" của US-12
- **AC3 — Công tắc chỉ hiển thị ở trạm có máy**
  - Given trạm có `UseArduino = true`
  - When mở màn hình Andon
  - Then hiển thị công tắc chế độ (kèm trạng thái hiện tại) trên `AndonBoardWindow`; trạm `UseArduino = false` không hiển thị
- **AC4 — Đổi công tắc cần Tổ trưởng + lý do + ghi log**
  - Given người dùng bấm công tắc
  - When đăng nhập nâng quyền (ADR-005) bằng tài khoản có permission `TesterMode.Change`, **nhập lý do** (bắt buộc — chọn từ danh sách gợi ý hoặc tự gõ, ô `ComboBox` editable giống cách nhập lý do ở Chế độ Scan NG US-18) và xác nhận
  - Then trạng thái đổi trên server và ghi 1 bản ghi log đổi chế độ gồm: trạm, từ chế độ, đến chế độ, lý do, người duyệt, thời điểm
- **AC5 — Từ chối đổi khi thiếu điều kiện**
  - Given người dùng bấm công tắc
  - When đăng nhập sai / thiếu permission / không chọn lý do / bấm Hủy
  - Then công tắc giữ nguyên, **không** ghi log đổi chế độ, hiển thị thông báo lý do từ chối
- **AC6 — Công tắc TẮT = Bypass: vẫn lưu Result = Ok, đánh dấu Bypass** *(quyết định 12/10/2026)*
  - Given trạm có `UseArduino = true` và công tắc = TẮT
  - When công nhân scan tem hợp lệ (qua kiểm tra FR-08)
  - Then lưu `Scan` với `Result = Ok` kèm `TesterMode = Bypass`, **tính là ĐÃ QUA công đoạn** (công đoạn sau scan tiếp được, tính vào ACTUAL); không gửi/nhận gì với máy; **không** hiển thị banner BYPASS; báo cáo/tra cứu lọc riêng được theo `TesterMode`
- **AC7 — Công tắc TẮT: App ngừng giao tiếp máy**
  - Given công tắc chuyển BẬT → TẮT
  - When đổi thành công
  - Then App đóng cổng COM, ngừng PING; máy tự chạy độc lập sau tối đa 5 giây (firmware không cần biết công tắc)
- **AC8 — Công tắc TẮT → BẬT**
  - Given công tắc chuyển TẮT → BẬT
  - When đổi thành công
  - Then App mở lại cổng COM, kết nối và đồng bộ theo US-14; trong lúc chưa kết nối xong, quét bị chặn (US-14/AC3)
- **AC9 — Không đổi công tắc khi đang có kết quả chờ**
  - Given App đang ở `WaitScan`/`Binding`/`Saving` (US-12)
  - When Tổ trưởng cố đổi công tắc
  - Then bị từ chối, thông báo "Hủy kết quả đang chờ trước khi đổi chế độ" (chốt 12/10/2026)
- **AC10 — Trạng thái công tắc nằm trên server, bền vững**
  - Given công tắc đã đổi
  - When tắt/mở lại App hoặc khởi động lại PC trạm
  - Then App đọc lại đúng trạng thái từ server; mặc định BẬT khi trạm mới được bật `UseArduino = true` (chốt 12/10/2026)
- **AC11 — Không đọc/ghi được server**
  - Given server không truy cập được
  - When Tổ trưởng đổi công tắc → không đổi, báo lỗi, không ghi log
  - And khi App khởi động ở trạm có máy mà không đọc được trạng thái công tắc → **chặn quét** + báo lỗi "Không xác định được chế độ máy thông điện" (tránh vô tình chạy nhầm Bypass hoặc ngược lại)
- **AC12 — Các trường COM trên `WorkStation` không còn dùng để kết nối**
  - Given `WorkStation.ComPort`/`BaudRate`/`CommandProtocol` (US-04/AC2)
  - When App kết nối máy
  - Then cấu hình kết nối lấy từ `appsettings.json` cục bộ tại trạm (section `Tester`, xem US-14); 3 trường trên server **không dùng nữa** (chuyển về cấu hình cục bộ). Không còn bắt buộc nhập khi `UseArduino = true`

**Nguồn FR:** FR-11 (viết lại 12/10/2026), mục 6 quy tắc 10, AC-20, AC-19c
**Phụ thuộc:** US-04 (cờ `UseArduino`; **US-04/AC2 cần sửa** vì bỏ bắt buộc ComPort/BaudRate/CommandProtocol), US-22 + ADR-004 (permission `TesterMode.Change`), ADR-005 (đăng nhập nâng quyền tại trạm), US-07/US-08 (luồng scan thường, Bypass dùng lại), US-12, US-14
**Cờ cảnh báo mục 8.2:** Có — điểm mở #2 (danh sách công đoạn dùng máy): hiện chỉ Thông điện; cần Admin bật `UseArduino` đúng trạm Thông điện khi triển khai.

## Ghi chú lưu trữ (cho dev tham khảo)
- Thêm cột vào `Scan` (không tạo bảng test_record riêng): `TesterMode` (`None`/`Tester`/`Bypass`), `TesterBootId`, `TesterSeq`, `GoldenCrc`, `FirmwareVersion`, `TestedAtUtc` — chi tiết xem US-12. Luồng thường (trạm không có máy) lưu `TesterMode = None`.
- Cần: nơi lưu trạng thái công tắc theo trạm trên server; bảng log đổi chế độ (trạm, từ, đến, lý do, người duyệt, thời điểm).
- Permission mới (ADR-004): `TesterMode.Change`; danh sách lý do cho phép chọn từ danh mục cố định hoặc nhập tự do — chưa chốt (xem câu hỏi mở).

## Quyết định đã chốt (12/10/2026)
- Lý do đổi chế độ: chọn từ danh sách gợi ý hoặc tự gõ (giống lý do ở Chế độ Scan NG).
- AC9 (không đổi khi đang có kết quả chờ), AC10 (mặc định BẬT), AC11 (không đọc được trạng thái công tắc → chặn quét): người giao việc đồng ý.

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần (code + test xong, chưa kiểm tra trực quan UI đầy đủ / chưa nghiệm thu với máy thật)
- **Cập nhật:** 2026-10-12

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-12 (ba, viết lại theo spec máy thông điện ESP32)
- Viết lại toàn bộ story: bỏ "cờ `SuDungArduino` kích hoạt state machine chờ OK/timeout 45s"; thay bằng 2 cờ (`UseArduino` có sẵn + công tắc runtime lưu server), log đổi chế độ, chế độ Bypass (`Result = Ok` + `TesterMode = Bypass`, tính là đã qua công đoạn, không banner).
- Ghi nhận `ComPort`/`BaudRate`/`CommandProtocol` trên `WorkStation` không còn dùng cho kết nối (chuyển về `appsettings.json` cục bộ, section `Tester`).
- Chưa có code triển khai.

#### 2026-10-12 (người giao việc chốt, ghi bởi agent chính)
- Chốt các giả định của BA: AC9, AC10 (mặc định BẬT), AC11 (chặn quét khi không đọc được công tắc); lý do đổi chế độ = gợi ý + tự gõ. Bỏ mục "Câu hỏi mở".

#### 2026-10-12 (dev, triển khai công tắc "Đang sử dụng máy thông điện" / Bypass) — commit `f990cd2` (backend + web-admin), `102cb15` (Station.Wpf)
- **Domain:** `WorkStation.TesterEnabled` (mặc định `true` = BẬT), `Scan.TesterMode` (enum `TesterMode`: `None`/`Tester`/`Bypass`, mặc định `None`), entity `TesterModeChangeLog` (trạm, từ/đến chế độ, lý do, người duyệt, thời điểm); permission mới `TesterMode.Change` + `TesterResult.Discard` (enum `PermissionResource`/`PermissionAction`).
- **Application:** `ITesterStationService` — `GetStateAsync` (UseArduino + công tắc), `SetEnabledAsync` (bắt buộc lý do, chỉ trạm `UseArduino = true`, đã ở đúng giá trị thì không ghi log). `ScanService.CreateAsync`: trạm có máy + công tắc BẬT => ném `BusinessRuleException` (409, phải đi luồng máy US-12); trạm có máy + công tắc TẮT => lưu `Result = Ok` + `TesterMode = Bypass` (vẫn qua FR-08, tính vào ACTUAL); trạm không có máy => `None`. `ScanHistoryQuery.TesterMode` + `ScanHistoryItemDto/ScanResultDto.TesterMode` (lọc/tra cứu riêng theo chế độ, query `?testerMode=Bypass`). Validator `CreateWorkStationRequest/UpdateWorkStationRequest` bỏ bắt buộc `ComPort/BaudRate/CommandProtocol` (AC12); validator `SetTesterEnabledRequest` (lý do bắt buộc).
- **Infrastructure:** migration `AddTesterModeSwitchAndScanTesterMode` (cột `WorkStation.TesterEnabled`, `Scan.TesterMode`, bảng `TesterModeChangeLog`); `DbSeeder` seed 2 permission mới + `EnsureTesterPermissionGrantsAsync` cấp cho Supervisor/Admin/Manager (idempotent, chạy mỗi lần khởi động).
- **Api:** `TesterController` — `GET api/v1/tester/station-state` (StationApiKey, trạm đọc trạng thái của chính nó theo claim), `PUT api/v1/tester/enabled` (Bearer + policy `TesterMode.Change`, người duyệt lấy từ claim JWT).
- **web-admin:** form trạm bỏ các ô Cổng COM/Baud/Giao thức, đổi nhãn thành "Trạm có máy thông điện"; type `WorkStation.testerEnabled` (chỉ đọc).
- **Station.Wpf:** `AndonBoardViewModel` đọc `GET tester/station-state` khi khởi động và mỗi chu kỳ làm mới => chế độ `Unknown` (CHẶN quét + banner "Không xác định được chế độ máy thông điện", tự thử lại mỗi 5 giây) / `NoMachine` (luồng thường, không công tắc, không mở COM) / `Bypass` / `Active`. Công tắc "MÁY THÔNG ĐIỆN: BẬT/TẮT" chỉ hiện ở trạm `UseArduino`; bấm => từ chối ngay nếu đang có kết quả chờ (AC9) => đăng nhập Tổ trưởng (`LoginDialog`, permission `TesterMode.Change`; sai tài khoản/thiếu quyền/Hủy => giữ nguyên, không log — AC5) => overlay lý do (`ComboBox` editable: gợi ý + tự gõ, bắt buộc) => `PUT tester/enabled`. Đổi thành công: TẮT => `TesterStationController.Stop()` (đóng COM, ngừng PING — AC7); BẬT => `Start()` (mở COM + đồng bộ — AC8). Bỏ `ArduinoTimeoutSeconds` khỏi `StationOptions`/`appsettings.json`/màn "Cấu hình trạm".
- **Test:** `TesterStationServiceTests`, `ScanServiceTesterModeTests` (None/Bypass/chặn khi BẬT), `WorkStationAndTesterValidatorTests` — `dotnet test tests/ProductionMES.Application.Tests` 424/424 pass.
- **Build:** `dotnet build ProductionMES.sln` 0 lỗi 0 cảnh báo; `npm run build` (web-admin) OK.
- **Quyết định tự chọn (người giao việc chưa chốt chi tiết):**
  - Server là nơi quyết định `TesterMode` (client không gửi chế độ): tránh client sai/lệch gây lưu nhầm; scan thường ở trạm có máy + công tắc BẬT bị chặn 409 để không ai lưu OK bỏ qua máy mà không để lại dấu vết.
  - `PUT tester/enabled` nhận `WorkStationId` trong body (cùng pattern `ScanNgController`/`CreateNgScanRequest`) vì scheme Bearer không mang danh tính trạm.
  - Giữ nguyên 3 cột `ComPort/BaudRate/CommandProtocol` trên `WorkStation` (không drop, không còn dùng) để không mất dữ liệu cũ.
  - Không đọc được trạng thái công tắc => chặn quét ở MỌI trạm (kể cả trạm không có máy, vì lúc đó chưa biết trạm có máy hay không); đọc lỗi sau khi đã biết chế độ thì giữ chế độ hiện tại. Nếu server báo đổi `UseArduino`/công tắc trong lúc đang có kết quả chờ thì hoãn áp dụng tới lần đọc sau.
  - Danh sách lý do gợi ý là hằng số trong `AndonBoardViewModel.Tester.cs` (2 danh sách: khi TẮT / khi BẬT lại), vẫn tự gõ được.
  - Chưa làm màn hình xem `TesterModeChangeLog` (chỉ lưu DB) — chưa có AC.
- **Còn thiếu / lý do giữ 🟡:** người dùng chưa chạy thử UI bằng mắt trên trạm thật (đã smoke-test mở app + chụp màn hình với server giả: banner không xác định chế độ, trạng thái BẬT/TẮT, panel máy); chưa nghiệm thu S9/S10 với máy thật; nên chạy migration trên DB thật và đăng nhập thử các tài khoản Supervisor/Manager (permission mới).
