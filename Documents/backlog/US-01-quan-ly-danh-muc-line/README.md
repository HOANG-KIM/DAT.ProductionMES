# US-01: Quản lý danh mục Line
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** thêm/sửa/vô hiệu hóa Line sản xuất
**Để** thiết lập danh sách các dây chuyền vật lý làm nền tảng cấu hình kế hoạch, trạm, và các luồng scan sau này

**Acceptance Criteria**
- **AC1 — Thêm Line mới**
  - Given tôi là Admin đang ở màn hình quản lý danh mục Line
  - When tôi nhập tên, mô tả và lưu
  - Then hệ thống tạo mới 1 Line với trạng thái hoạt động mặc định
- **AC2 — Sửa thông tin Line**
  - Given Line đã tồn tại
  - When tôi cập nhật tên/mô tả và lưu
  - Then thông tin Line được cập nhật, không ảnh hưởng dữ liệu lịch sử scan đã gắn với Line đó
- **AC3 — Vô hiệu hóa Line**
  - Given Line đang hoạt động
  - When tôi chọn vô hiệu hóa
  - Then Line chuyển trạng thái ngưng hoạt động, không còn được chọn khi tạo kế hoạch sản xuất mới (FR-05), nhưng dữ liệu lịch sử liên quan vẫn giữ nguyên
- **AC4 — Bổ sung Line mới không cần deploy lại** (theo AC-07 và NFR "Khả năng mở rộng")
  - Given hệ thống đang vận hành
  - When Admin thêm Line mới qua giao diện cấu hình
  - Then Line mới có hiệu lực ngay, không cần sửa/deploy lại code

**Nguồn FR:** FR-01
**Phụ thuộc:** Không có (story nền tảng, làm đầu tiên)
**Cờ cảnh báo mục 8.2:** Có — số lượng Line thực tế chưa xác định, nhưng không ảnh hưởng thiết kế chức năng (hệ thống thiết kế cấu hình được).
**Ghi chú:** US-01 đã code xong (commit `d4dd6ab`) trước khi FR-01/FR-09a (khung giờ nghỉ) được bổ sung vào SRS ngày 13/08/2026 — phần khung giờ nghỉ tách thành story riêng **US-01a** ngay bên dưới, không gộp ngược vào US-01 để tránh nhầm là còn nằm trong scope chưa code.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

Backend + UI web-admin (`a42c9f2`, `d4dd6ab`)
