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

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-10-12

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-12 (ba, viết lại theo spec máy thông điện ESP32)
- Viết lại toàn bộ story: bỏ "cờ `SuDungArduino` kích hoạt state machine chờ OK/timeout 45s"; thay bằng 2 cờ (`UseArduino` có sẵn + công tắc runtime lưu server), log đổi chế độ, chế độ Bypass (`Result = Ok` + `TesterMode = Bypass`, tính là đã qua công đoạn, không banner).
- Ghi nhận `ComPort`/`BaudRate`/`CommandProtocol` trên `WorkStation` không còn dùng cho kết nối (chuyển về `appsettings.json` cục bộ, section `Tester`).
- Chưa có code triển khai.

#### 2026-10-12 (người giao việc chốt, ghi bởi agent chính)
- Chốt các giả định của BA: AC9, AC10 (mặc định BẬT), AC11 (chặn quét khi không đọc được công tắc); lý do đổi chế độ = gợi ý + tự gõ. Bỏ mục "Câu hỏi mở".
