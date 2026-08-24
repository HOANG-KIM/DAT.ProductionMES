# US-01a: Cấu hình khung giờ nghỉ theo Line
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình 0 hoặc nhiều khung giờ nghỉ (giờ bắt đầu, giờ kết thúc, ghi chú) cho từng Line
**Để** hệ thống có cơ sở trừ đúng thời gian nghỉ khi tính sản lượng kế hoạch lũy kế hiển thị tại màn hình trạm (FR-09a)

**Acceptance Criteria**
- **AC1 — Thêm khung giờ nghỉ cho Line**
  - Given tôi là Admin đang cấu hình 1 Line đã tồn tại (US-01)
  - When tôi thêm 1 khung giờ nghỉ (giờ bắt đầu, giờ kết thúc, ghi chú — vd nghỉ trưa 12:00–13:00, nghỉ giữa giờ 15:00–15:15) và lưu
  - Then khung giờ nghỉ được lưu, áp dụng chung cho **mọi kế hoạch sản xuất chạy trên Line đó** (không cấu hình riêng theo từng kế hoạch)
- **AC2 — Line có thể có nhiều khung giờ nghỉ**
  - Given Line đã có 1 khung giờ nghỉ (vd nghỉ trưa)
  - When Admin thêm thêm khung giờ nghỉ khác (vd nghỉ giữa giờ)
  - Then cả 2 khung đều được lưu và áp dụng đồng thời khi tính sản lượng kế hoạch lũy kế
- **AC3 — Sửa/xóa khung giờ nghỉ**
  - Given khung giờ nghỉ đã tồn tại
  - When Admin sửa giờ bắt đầu/kết thúc/ghi chú, hoặc xóa khung giờ nghỉ đó
  - Then thay đổi có hiệu lực ngay cho các lần tính sản lượng kế hoạch lũy kế tiếp theo, không ảnh hưởng số liệu lịch sử đã tính trước đó
- **AC4 — Line không cấu hình khung giờ nghỉ nào vẫn hoạt động bình thường**
  - Given 1 Line không có khung giờ nghỉ nào được cấu hình (0 khung giờ nghỉ)
  - When hệ thống tính sản lượng kế hoạch lũy kế cho Line đó (FR-09a)
  - Then tính liên tục theo thời gian làm việc thực tế đã trôi qua, không có phần trừ giờ nghỉ nào
- **AC5 — Từ chối khung giờ nghỉ không hợp lệ**
  - Given Admin đang thêm/sửa khung giờ nghỉ
  - When giờ kết thúc không lớn hơn giờ bắt đầu, hoặc khung giờ nghỉ mới chồng lấn 1 khung giờ nghỉ khác đã có của cùng Line
  - Then hệ thống từ chối lưu, báo lỗi rõ ràng

**Nguồn FR:** FR-01, FR-09a, mục 6 quy tắc 11
**Phụ thuộc:** US-01 (Line phải tồn tại trước). Là điều kiện tiên quyết để US-09 AC5/AC6 (trừ giờ nghỉ khi tính PLAN lũy kế tại màn hình trạm) tính đúng — nếu chưa có story này, US-09 chỉ thỏa được trường hợp "0 khung giờ nghỉ".
**Cờ cảnh báo mục 8.2:** Không.
**Ghi chú:** Story tách ra vì FR-01/FR-09a (khung giờ nghỉ) được bổ sung vào SRS **sau** khi US-01 đã code xong (13/08/2026 so với commit US-01 trước đó) — `Line.cs` hiện chưa có field nào cho khung giờ nghỉ.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

`4f0a3ed`
