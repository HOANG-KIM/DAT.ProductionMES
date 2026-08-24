# US-11: Cấu hình bật/tắt sử dụng Arduino theo từng trạm
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình cờ `SuDungArduino` (bật/tắt) độc lập cho từng trạm
**Để** trạm nào không có Arduino vẫn hoạt động bình thường theo luồng scan thủ công, không bị ảnh hưởng bởi logic Arduino

**Acceptance Criteria**
- **AC1 — Trạm không dùng Arduino hoạt động luồng thủ công bình thường** *(AC-20 gốc)*
  - Given trạm có `SuDungArduino = false`
  - When công nhân scan tem tại trạm này
  - Then hoạt động hoàn toàn theo luồng scan thủ công bình thường (FR-07/FR-08/FR-18), không có bước chờ Arduino *(AC-20)*
- **AC2 — Bật Arduino kích hoạt state machine**
  - Given trạm có `SuDungArduino = true`
  - When công nhân scan tem
  - Then trạm hoạt động theo state machine mô tả tại US-12 (FR-12), thay vì luồng thủ công đơn thuần

**Nguồn FR:** FR-11
**Phụ thuộc:** US-04 (quản lý trạm làm việc)
**Cờ cảnh báo mục 8.2:** Có — cần xác nhận danh sách công đoạn nào thực sự dùng Arduino (điểm mở #2) trước khi cấu hình `SuDungArduino = true` cho trạm cụ thể tại xưởng.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


