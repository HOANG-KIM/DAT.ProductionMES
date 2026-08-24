# US-06: Tính và hiển thị sản lượng chuẩn theo giờ
**Là** Tổ trưởng / Công nhân vận hành trạm
**Tôi muốn** hệ thống tự động tính sản lượng chuẩn theo giờ từ takt time
**Để** biết ngay tốc độ sản xuất mục tiêu mà không cần tính tay

**Acceptance Criteria**
- **AC1 — Công thức tính sản lượng chuẩn** *(AC-04 gốc)*
  - Given nhập takt time = 30 giây
  - When hệ thống tính toán
  - Then hiển thị sản lượng chuẩn = 120 sản phẩm/giờ *(AC-04)*
- **AC2 — Hiển thị trên màn hình cấu hình kế hoạch**
  - Given kế hoạch đã có takt time
  - When Tổ trưởng xem màn hình cấu hình kế hoạch
  - Then sản lượng/giờ (`3600 / Takt time`) hiển thị ngay bên cạnh takt time
- **AC3 — Hiển thị trên màn hình trạm**
  - Given trạm đang có kế hoạch active
  - When công nhân xem màn hình trạm
  - Then sản lượng chuẩn/giờ tương ứng với công đoạn/kế hoạch đó cũng được hiển thị

**Nguồn FR:** FR-06
**Phụ thuộc:** US-05 (kế hoạch sản xuất phải có takt time)
**Cờ cảnh báo mục 8.2:** Không.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)

Xác nhận có sẵn từ US-05, không cần code thêm (ghi chú trong `4f0a3ed`)
