# US-04: Quản lý trạm làm việc
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình trạm làm việc gắn với 1 Line và 1 công đoạn cụ thể, kèm thông tin cổng COM nếu có Arduino
**Để** mỗi trạm hoạt động đúng phạm vi nghiệp vụ của nó (Line/công đoạn) và có đủ thông tin kết nối phần cứng khi cần

**Acceptance Criteria**
- **AC1 — Tạo trạm làm việc**
  - Given Line và công đoạn đã tồn tại trong danh mục
  - When Admin tạo trạm mới, chọn đúng 1 Line và 1 công đoạn
  - Then trạm được tạo, gắn cố định với Line/công đoạn đã chọn
- **AC2 — Cấu hình cổng COM khi trạm có Arduino**
  - Given trạm được đánh dấu có sử dụng Arduino (`SuDungArduino = true`, xem US-11)
  - When Admin nhập cổng COM, baud rate, giao thức lệnh
  - Then thông tin được lưu và dùng để trạm WPF kết nối Serial khi khởi động
- **AC3 — Trạm không dùng Arduino không yêu cầu cấu hình COM**
  - Given trạm có `SuDungArduino = false`
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
