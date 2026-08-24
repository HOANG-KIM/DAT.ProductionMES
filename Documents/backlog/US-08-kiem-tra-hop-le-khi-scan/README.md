# US-08: Kiểm tra hợp lệ khi scan (chống trùng tem & kiểm tra công đoạn liền trước)
**Là** Hệ thống (business rule nền tảng), phục vụ Công nhân/Tổ trưởng
**Tôi muốn** kiểm tra trùng tem theo công đoạn và kiểm tra đã qua công đoạn liền trước, trên phạm vi toàn hệ thống (không phân biệt Line)
**Để** đảm bảo tính toàn vẹn dữ liệu sản xuất, tránh ghi nhận sai lệch sản lượng và trình tự công đoạn

**Acceptance Criteria**
- **AC1 — Trùng tem cùng công đoạn khác Line** *(AC-01 gốc)*
  - Given scan tem A tại công đoạn "Lắp ráp" Line 1
  - When sau đó scan tem A tại công đoạn "Lắp ráp" Line 2
  - Then lần 2 bị từ chối, báo "Trùng tem tại công đoạn này" *(AC-01)*
- **AC2 — Khác công đoạn, khác Line hợp lệ** *(AC-02 gốc)*
  - Given scan tem A tại "Lắp ráp" Line 1
  - When sau đó scan tem A tại "Thông điện" Line 2
  - Then lần 2 được chấp nhận *(AC-02)*
- **AC3 — Chưa qua công đoạn liền trước** *(AC-03 gốc)*
  - Given tem B chưa từng scan "Lắp ráp" ở bất kỳ Line nào
  - When scan tem B tại "Thông điện"
  - Then bị từ chối, báo rõ "Chưa qua công đoạn: Lắp ráp" *(AC-03)*
- **AC4 — Scan hợp lệ khi qua đủ 2 bước kiểm tra**
  - Given tem chưa trùng công đoạn hiện tại và đã qua công đoạn liền trước
  - When công nhân scan
  - Then lượt scan được ghi nhận OK
- **AC5 — Không xử lý race condition giữa 2 Line cho cùng 1 tem** (theo mục 8.1 SRS)
  - Given tem chỉ tồn tại vật lý ở đúng 1 vị trí tại 1 thời điểm
  - When xử lý transaction ghi nhận lượt scan
  - Then xử lý theo transaction thông thường, không cần cơ chế khóa/đồng bộ đặc biệt cho race condition 2 Line

**Nguồn FR:** FR-08, mục 6 quy tắc 3-4, mục 8.1
**Phụ thuộc:** US-03 (trình tự công đoạn — để xác định công đoạn "liền trước"), US-07 (luồng scan cơ bản)
**Cờ cảnh báo mục 8.2:** Không trực tiếp (rule đã chốt rõ ràng).

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

`ceb0ee1` — rule backend, không có UI riêng
