# US-04: Quản lý trạm làm việc
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình trạm làm việc gắn với 1 Line và 1 công đoạn cụ thể, và đánh dấu trạm có máy thông điện hay không
**Để** mỗi trạm hoạt động đúng phạm vi nghiệp vụ của nó (Line/công đoạn) và có đủ thông tin kết nối phần cứng khi cần

**Acceptance Criteria**
- **AC1 — Tạo trạm làm việc**
  - Given Line và công đoạn đã tồn tại trong danh mục
  - When Admin tạo trạm mới, chọn đúng 1 Line và 1 công đoạn
  - Then trạm được tạo, gắn cố định với Line/công đoạn đã chọn
- **AC2 — Đánh dấu trạm có máy thông điện** *(sửa 12/10/2026)*
  - Given Admin cấu hình trạm
  - When bật cờ "Trạm có máy thông điện" (`UseArduino = true`, xem US-11)
  - Then cờ được lưu; **không bắt buộc** nhập cổng COM/baud rate/giao thức lệnh — cấu hình kết nối máy nằm ở `appsettings.json` cục bộ tại trạm (section `Tester`, US-14). Các trường `ComPort`/`BaudRate`/`CommandProtocol` trên server không còn dùng cho kết nối
- **AC3 — Trạm không dùng máy thông điện không yêu cầu cấu hình COM**
  - Given trạm có `UseArduino = false`
  - When Admin cấu hình trạm
  - Then hệ thống không bắt buộc nhập thông tin cổng COM

**Nguồn FR:** FR-04
**Phụ thuộc:** US-01 (Line), US-02 (Công đoạn)
**Cờ cảnh báo mục 8.2:** Có — model máy scan (điểm mở #3) và danh sách công đoạn dùng Arduino (điểm mở #2) ảnh hưởng trực tiếp tới cấu hình trạm; cần xác nhận trước khi triển khai thực tế tại xưởng (không ảnh hưởng phần thiết kế chức năng chung).

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

Backend + UI web-admin (`a42c9f2`, `cedf66b`)

#### 2026-10-12 (ba, sửa AC2 theo spec máy thông điện ESP32)
- **Spec:** AC2 đổi từ "bắt buộc nhập cổng COM/baud/giao thức khi `UseArduino = true`" thành "chỉ đánh dấu trạm có máy"; cấu hình kết nối chuyển về `appsettings.json` cục bộ tại trạm (US-14). Đổi tên `SuDungArduino` → `UseArduino` (tên thật trong code).
- **Còn thiếu:** code hiện tại vẫn bắt buộc 3 trường COM khi `UseArduino = true` (`CreateWorkStationRequestValidator`/`UpdateWorkStationRequestValidator` + form web-admin) — gỡ cùng lúc implement US-11 (AC12). Giữ trạng thái ✅ vì phần quản lý trạm vẫn dùng được, chỉ thừa ràng buộc.
