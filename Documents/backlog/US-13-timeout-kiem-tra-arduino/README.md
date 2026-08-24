# US-13: Timeout xác định kết quả kiểm tra Arduino
**Là** Hệ thống / Admin cấu hình trạm
**Tôi muốn** đếm timeout kể từ lúc scan tem và suy luận NG khi hết thời gian mà không nhận "OK"
**Để** hệ thống không bị treo vô thời hạn chờ tín hiệu Arduino trong trường hợp thiết bị không đạt

**Acceptance Criteria**
- **AC1 — Timeout mặc định 45 giây**
  - Given trạm dùng Arduino chưa có cấu hình riêng
  - When tính timeout chờ kết quả
  - Then hệ thống áp dụng mặc định 45 giây
- **AC2 — Cấu hình timeout riêng theo từng trạm qua file cấu hình cục bộ**
  - Given file cấu hình cục bộ (`appsettings.json`) tại 1 trạm có giá trị timeout khác mặc định
  - When trạm khởi động và chờ kết quả Arduino
  - Then áp dụng đúng giá trị đã cấu hình riêng cho trạm đó, không cần sửa code/deploy lại
- **AC3 — Hết timeout chuyển sang bước xác nhận NG**
  - Given đã scan tem và bắt đầu đếm timeout
  - When hết thời gian timeout mà không nhận "OK"
  - Then hệ thống chuyển sang bước xác nhận NG (AC4 của US-12 / AC-19)

**Nguồn FR:** FR-13
**Phụ thuộc:** US-12 (là 1 phần cấu thành state machine của US-12, có thể coi là sub-story kỹ thuật gắn liền)
**Cờ cảnh báo mục 8.2:** Không trực tiếp, nhưng giá trị timeout đã chốt cấu hình cục bộ per-trạm (không phải điểm mở).

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


