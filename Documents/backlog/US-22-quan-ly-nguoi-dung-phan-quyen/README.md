# US-22: Quản lý người dùng & phân quyền
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** quản lý tài khoản người dùng và phân quyền theo 4 nhóm vai trò
**Để** đảm bảo mỗi người dùng chỉ thực hiện đúng chức năng thuộc phạm vi vai trò của mình

**Acceptance Criteria**
- **AC1 — Phân quyền theo 4 nhóm vai trò**
  - Given hệ thống có 4 nhóm người dùng: Công nhân vận hành trạm, Tổ trưởng/Quản lý chuyền, Admin, Ban quản lý/Văn phòng
  - When Admin gán vai trò cho tài khoản
  - Then mỗi tài khoản chỉ truy cập được các chức năng đúng phạm vi vai trò tương ứng (mục 2.2 SRS)
- **AC2 — Quyền riêng cho thao tác Mở khóa rework**
  - Given tài khoản có vai trò Công nhân
  - When cố gắng thực hiện thao tác "Mở khóa rework" (FR-19)
  - Then hệ thống từ chối, chỉ tài khoản vai trò Tổ trưởng mới thực hiện được
- **AC3 — Không có vai trò QC riêng biệt**
  - Given hệ thống chỉ định nghĩa 4 vai trò theo mục 2.2 SRS
  - When cấu hình phân quyền
  - Then không tồn tại vai trò "QC" độc lập — mọi chức năng trước đây gắn với "QC" thuộc về vai trò Tổ trưởng

**Nguồn FR:** FR-22, mục 8.1 (quyết định gộp QC vào Tổ trưởng)
**Phụ thuộc:** Không có ràng buộc cứng về thứ tự dữ liệu nghiệp vụ, nhưng nên có sớm vì nhiều story khác (US-19, US-12) cần cơ chế phân quyền/đăng nhập Tổ trưởng để hoạt động đúng.
**Cờ cảnh báo mục 8.2:** Không.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

Backend + UI web-admin (`a42c9f2`, `cedf66b`)
