# US-26: Theo dõi tiến độ đóng thùng ở mức quản lý
**Là** Tổ trưởng / Ban quản lý (Manager)
**Tôi muốn** xem tổng quan tiến độ đóng thùng của các kế hoạch sản xuất đang chạy, đối chiếu với Tổng số lượng Lot
**Để** biết đã đóng gói được bao nhiêu thùng/bao nhiêu % so với tổng đơn hàng, phục vụ điều phối và báo cáo

**Acceptance Criteria**
- **AC1 — Danh sách tổng quan các kế hoạch đang đóng thùng**
  - Given có ít nhất 1 kế hoạch sản xuất đang ở trạng thái Running tại công đoạn "Đóng thùng" (bất kỳ Line nào)
  - When tôi mở màn hình theo dõi tiến độ đóng thùng
  - Then thấy danh sách các dòng tương ứng, mỗi dòng gồm tối thiểu: Line, Model, Lot, số thùng đã đóng, tổng số lượng sản phẩm đã đóng thùng OK
- **AC2 — Đối chiếu % hoàn thành theo Lot**
  - Given Lot của kế hoạch đã có "Tổng số lượng Lot" được nhập tay (US-21a)
  - When tôi xem dòng tương ứng
  - Then hệ thống hiển thị % hoàn thành = (tổng số lượng sản phẩm đã đóng thùng OK / Tổng số lượng Lot) × 100, kèm nhãn **Đủ/Chưa đủ** khi đạt 100% — dùng lại đúng quy ước hiển thị đã có ở báo cáo Lot-centric (US-21/AC6, US-21a), KHÔNG có trạng thái/khái niệm "hoàn thành" riêng nào khác *(chốt 20/08/2026)*
- **AC3 — Lot chưa có Tổng số lượng Lot**
  - Given Lot của kế hoạch chưa được nhập "Tổng số lượng Lot" (US-21a)
  - When tôi xem dòng tương ứng
  - Then hệ thống hiển thị rõ trạng thái "Chưa xác định" cho % hoàn thành, không suy diễn hay để trống gây hiểu nhầm là 0%
- **AC4 — Không giới hạn theo 1 Line**
  - Given nhiều Line đang chạy nhiều kế hoạch đóng thùng khác nhau
  - When tôi mở màn hình
  - Then thấy được tổng quan trên toàn nhà máy, có thể lọc theo Line/Lot/Model nếu cần thu hẹp
- **AC5 — Dữ liệu cập nhật gần thời gian thực**
  - Given màn hình theo dõi tiến độ đang mở
  - When có lượt đóng thùng mới hoàn tất ở bất kỳ trạm nào
  - Then số liệu trên màn hình được cập nhật trong thời gian ngắn mà không cần người dùng bấm làm mới thủ công (mức độ real-time tuyệt đối không bắt buộc, khác NFR real-time ≤1s đã áp cho US-09)

**Nguồn FR:** FR-26
**Phụ thuộc:** US-25 (cần có dữ liệu đóng thùng thực tế để tổng hợp), US-21a (`Lot.TotalQuantity` — nguồn đối chiếu % hoàn thành AC2/AC3), US-21 (báo cáo Lot-centric — có thể tái dùng `GET api/v1/scans/history` làm nền tham khảo, không bắt buộc)
**Cờ cảnh báo mục 8.2:** Không còn — đã chốt 20/08/2026 (xem AC2).

**Ghi chú chung 3 story (US-24/25/26):**
- **Tất cả 5 điểm mở phát sinh đã được chốt 20/08/2026** (không còn cờ cảnh báo mục 8.2 nào cho US-24/25/26): (1) Model khớp `ProductionPlan.Model` không phân biệt hoa/thường + tự trim + autocomplete, CHỈ áp dụng riêng bước tra cứu cấu hình đóng gói, không đổi cách dùng Model ở nơi khác (US-24/AC9); (2) Supervisor xác nhận tem trùng KHÔNG cộng thêm số lượng, chỉ audit (US-25/AC8); (3) máy in lỗi KHÔNG chặn đóng thùng kế tiếp, luôn có In lại thủ công (US-25/AC13); (4) sửa Quy cách đóng gói không hồi tố cho thùng đang dở, snapshot lúc mở thùng (US-25/AC12); (5) tiến độ theo Lot không có trạng thái riêng, chỉ %/nhãn Đủ-Chưa đủ dùng lại từ US-21/US-21a (US-26/AC2).
- **Cơ chế in tem**: giữ nguyên theo mẫu Excel template (quyết định kỹ thuật đã chốt với người giao việc ngày 20/08/2026, không đổi sang máy in tem chuyên dụng) — thuộc phạm vi `dev` khi implement, không phải nội dung đặc tả BA.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-20

## Lịch sử triển khai (ghi chú backlog)

Story mới 20/08/2026 — phụ thuộc US-25 (cần có dữ liệu đóng thùng thực tế) + US-21a (`Lot.TotalQuantity` để tính %). Xem AC đầy đủ ở mục 3.8, nguồn FR-26 (SRS)
