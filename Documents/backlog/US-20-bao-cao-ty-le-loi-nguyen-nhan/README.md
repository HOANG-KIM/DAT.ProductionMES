# US-20: Báo cáo tỷ lệ lỗi & nguyên nhân
**Là** Ban quản lý / Tổ trưởng
**Tôi muốn** xem thống kê số lượng/tỷ lệ NG theo công đoạn, Line, loại lỗi, khoảng thời gian
**Để** phân tích chất lượng sản xuất và xác định nguyên nhân lỗi thường gặp

**Acceptance Criteria**
- **AC1 — Thống kê theo công đoạn/Line**
  - Given có dữ liệu lịch sử NG (từ US-18/US-19)
  - When người dùng chọn bộ lọc theo công đoạn hoặc Line
  - Then hiển thị số lượng/tỷ lệ NG tương ứng
- **AC2 — Thống kê theo loại lỗi**
  - Given lịch sử NG có kèm lý do lỗi (free text)
  - When người dùng xem báo cáo theo loại lỗi
  - Then hệ thống nhóm/thống kê theo các lý do lỗi đã ghi nhận
- **AC3 — Thống kê theo khoảng thời gian**
  - Given người dùng chọn 1 khoảng thời gian
  - When xem báo cáo
  - Then chỉ số NG được tính trong đúng khoảng thời gian đã chọn
- **AC4 — Đặt tại màn hình báo cáo, không phải màn hình trạm**
  - Given người dùng là Ban quản lý/Tổ trưởng
  - When cần xem thống kê tỷ lệ lỗi
  - Then truy cập qua màn hình báo cáo riêng, không hiển thị tại màn hình trạm vận hành

**Nguồn FR:** FR-20
**Phụ thuộc:** US-18, US-19 (cần đủ dữ liệu NG/OK để thống kê)
**Cờ cảnh báo mục 8.2:** Không trực tiếp, nhưng liên quan tới điểm mở #4 (nội dung báo cáo Excel) khi xuất báo cáo này ra Excel (xem US-23).

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


