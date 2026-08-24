# US-17: Hiển thị rõ ràng trạng thái đồng bộ trên UI
**Là** Công nhân vận hành trạm / Tổ trưởng
**Tôi muốn** phân biệt rõ 3 trạng thái đồng bộ của mỗi lượt scan bằng màu sắc
**Để** biết chắc lượt nào đã được server xác nhận chính thức, tránh nhầm lẫn khi tính sản lượng

**Acceptance Criteria**
- **AC1 — Phân biệt 3 trạng thái bằng màu**
  - Given danh sách/số lượng lượt scan tại màn hình trạm
  - When hiển thị dữ liệu
  - Then phân biệt rõ 3 trạng thái: `Đã xác nhận OK` (xanh), `Đã xác nhận NG` (đỏ), `Chờ đồng bộ` (vàng/xám)
- **AC2 — Không tính lượt Chờ đồng bộ vào sản lượng chính thức**
  - Given có lượt scan ở trạng thái `Chờ đồng bộ`
  - When hiển thị số lượng/chỉ số +/-
  - Then lượt này chưa được tính vào sản lượng chính thức cho tới khi server xác nhận

**Nguồn FR:** FR-17
**Phụ thuộc:** US-16 (hàng đợi cục bộ — nguồn dữ liệu trạng thái đồng bộ)
**Cờ cảnh báo mục 8.2:** Không.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


