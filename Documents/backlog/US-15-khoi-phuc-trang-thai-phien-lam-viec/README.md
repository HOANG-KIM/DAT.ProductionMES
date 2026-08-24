# US-15: Khôi phục trạng thái phiên làm việc khi khởi động lại (tắt/mở bình thường)
**Là** Công nhân vận hành trạm
**Tôi muốn** ứng dụng trạm tự động hiển thị lại đúng kế hoạch, số lượng, chỉ số +/- khi mở lại
**Để** không phải chọn lại cấu hình từ đầu sau khi tắt/mở ứng dụng bình thường

**Acceptance Criteria**
- **AC1 — Khôi phục đúng trạng thái sau khi mở lại** *(AC-09 gốc)*
  - Given tắt ứng dụng bình thường rồi mở lại
  - When ứng dụng khởi động, gọi API lấy lại trạng thái từ server
  - Then hiển thị lại đúng kế hoạch, số lượng, chỉ số +/- như trước khi tắt, không cần chọn lại cấu hình *(AC-09)*
- **AC2 — Dữ liệu nguồn từ server, UI chỉ đồng bộ lại**
  - Given dữ liệu kế hoạch/số lượng/chỉ số đã lưu tại server (MySQL)
  - When ứng dụng khởi động lại
  - Then chỉ cần đồng bộ giao diện đúng theo trạng thái server, không tính toán lại từ đầu phía client

**Nguồn FR:** FR-15
**Phụ thuộc:** US-05 (kế hoạch active), US-09 (số lượng/chỉ số +/-)
**Cờ cảnh báo mục 8.2:** Không.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


