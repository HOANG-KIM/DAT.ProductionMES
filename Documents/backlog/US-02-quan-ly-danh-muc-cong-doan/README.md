# US-02: Quản lý danh mục Công đoạn (master)
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** thêm/sửa/vô hiệu hóa công đoạn dùng chung cho toàn hệ thống
**Để** có danh mục công đoạn (Lắp ráp, Thông điện, Ngoại quan…) tái sử dụng được cho nhiều Line khác nhau

**Acceptance Criteria**
- **AC1 — Thêm công đoạn master**
  - Given tôi là Admin
  - When tôi nhập tên công đoạn mới và lưu
  - Then công đoạn được thêm vào danh mục dùng chung, chưa gắn với Line/kế hoạch nào
- **AC2 — Công đoạn không gắn cố định 1 Line**
  - Given công đoạn "Thông điện" đã có trong danh mục
  - When công đoạn này được áp dụng vào kế hoạch của Line 1 và Line 2
  - Then cả 2 Line đều dùng chung 1 định danh công đoạn "Thông điện" (phục vụ đúng rule chống trùng tem toàn hệ thống ở FR-08)
- **AC3 — Vô hiệu hóa công đoạn**
  - Given công đoạn đang hoạt động và không còn được dùng trong kế hoạch active nào
  - When Admin vô hiệu hóa
  - Then công đoạn không còn xuất hiện trong danh sách chọn khi cấu hình kế hoạch mới (FR-03)

**Nguồn FR:** FR-02
**Phụ thuộc:** Không có (song song với US-01, có thể làm ngay sau/cùng US-01)
**Cờ cảnh báo mục 8.2:** Có — danh sách công đoạn cụ thể từng Line và công đoạn nào dùng Arduino chưa xác định; ảnh hưởng tới US-04 (SuDungArduino) khi cần biết chính xác công đoạn nào cần bật cờ này.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

Backend + UI web-admin (`a42c9f2`, `cedf66b`)
