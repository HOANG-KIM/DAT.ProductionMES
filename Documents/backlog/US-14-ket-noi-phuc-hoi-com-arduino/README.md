# US-14: Kết nối & phục hồi cổng COM với Arduino
**Là** Công nhân vận hành trạm / Hệ thống
**Tôi muốn** ứng dụng trạm tự động mở và phục hồi kết nối cổng COM, đồng thời ghi log dữ liệu Serial
**Để** đảm bảo kết quả OK/NG từ Arduino luôn đáng tin cậy và có thể truy vết khi có sự cố phần cứng

**Acceptance Criteria**
- **AC1 — Tự động mở kết nối khi khởi động**
  - Given trạm có `SuDungArduino = true`
  - When ứng dụng WPF khởi động
  - Then tự động mở kết nối cổng COM theo cấu hình đã lưu (FR-04)
- **AC2 — Tự kết nối lại khi mất kết nối**
  - Given kết nối COM bị mất trong lúc vận hành
  - When hệ thống phát hiện mất kết nối
  - Then tự động thử kết nối lại, không cần khởi động lại ứng dụng thủ công
- **AC3 — Không cho scan trong lúc mất kết nối**
  - Given cổng COM đang mất kết nối tại trạm dùng Arduino
  - When công nhân cố gắng scan tem
  - Then hệ thống hiển thị rõ trạng thái lỗi thiết bị và không cho scan (vì không thể tin cậy kết quả OK/NG lúc này)
- **AC4 — Log toàn bộ dữ liệu gửi/nhận Serial**
  - Given trạm đang giao tiếp Serial với Arduino
  - When có dữ liệu gửi hoặc nhận qua cổng COM
  - Then hệ thống ghi log lại (bảng `LichSuLenhArduino`) phục vụ truy vết sự cố phần cứng

**Nguồn FR:** FR-14
**Phụ thuộc:** US-04 (cấu hình COM tại trạm), US-11
**Cờ cảnh báo mục 8.2:** Không trực tiếp.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


