# US-12: Luồng "Scan tem trước → chờ kết quả kiểm tra từ Arduino" khi có Arduino
**Là** Công nhân vận hành trạm (công đoạn có Arduino, vd Thông điện)
**Tôi muốn** scan tem trước rồi hệ thống tự chờ và ghi nhận kết quả kiểm tra từ Arduino
**Để** đảm bảo kết quả kiểm tra tự động (OK/NG) được gán đúng cho đúng sản phẩm, không nhầm lẫn

**Acceptance Criteria**
- **AC1 — Chuyển trạng thái chờ sau khi scan** *(AC-17 gốc)*
  - Given trạm có `SuDungArduino = true`
  - When công nhân scan tem
  - Then chuyển sang trạng thái "Đang chờ kết quả kiểm tra" cho đúng tem đó; không cho scan tem khác trong lúc chờ *(AC-17)*
- **AC2 — Kiểm tra trùng tem/công đoạn trước ngay tại thời điểm scan**
  - Given công nhân scan tem tại trạm dùng Arduino
  - When hệ thống nhận lượt scan
  - Then vẫn thực hiện đầy đủ kiểm tra trùng tem/công đoạn trước (FR-08) ngay lúc scan, trước khi chuyển sang trạng thái chờ; nếu không hợp lệ thì từ chối như luồng thường, không chuyển sang chờ Arduino
- **AC3 — Nhận OK trước khi hết timeout, lưu tự động** *(AC-18 gốc)*
  - Given Arduino gửi "OK" sau khi đã scan tem, trước khi hết timeout
  - When hệ thống nhận tín hiệu
  - Then tự động lưu lịch sử OK ngay, không cần xác nhận thêm; trạng thái quay lại "Sẵn sàng scan" *(AC-18)*
- **AC4 — Hết timeout, suy luận NG, yêu cầu Tổ trưởng xác nhận** *(AC-19 gốc)*
  - Given hết 45 giây kể từ lúc scan tem mà không nhận được "OK" từ Arduino
  - When timeout xảy ra
  - Then hệ thống suy luận NG, hiển thị popup ngay tại trạm yêu cầu Tổ trưởng xác nhận "Lưu NG" hoặc "Hủy — kiểm tra lại" (cần đăng nhập Tổ trưởng) *(AC-19)*
- **AC5 — Tổ trưởng chọn "Xác nhận lưu NG"** *(AC-19b gốc)*
  - Given popup xác nhận NG đang hiển thị
  - When Tổ trưởng chọn "Xác nhận lưu NG"
  - Then ghi nhận lịch sử NG kèm lý do mặc định ("Lỗi thông điện (tự động từ thiết bị — không nhận phản hồi OK)"), tem bị khóa, cần mở khóa rework (US-19/FR-19) để scan lại *(AC-19b)*
- **AC6 — Tổ trưởng chọn "Hủy — kiểm tra lại"** *(AC-19c gốc)*
  - Given popup xác nhận NG đang hiển thị
  - When Tổ trưởng chọn "Hủy — kiểm tra lại"
  - Then không ghi nhận NG vào lịch sử, tem không bị khóa, trạng thái quay về "Đang chờ kết quả kiểm tra" cho đúng tem đó (đếm lại timeout), không cần scan lại tem *(AC-19c)*

**Nguồn FR:** FR-12, mục 6 quy tắc 10
**Phụ thuộc:** US-08 (rule kiểm tra hợp lệ), US-11 (cờ SuDungArduino), US-13 (timeout), US-14 (kết nối COM), US-19 (mở khóa rework)
**Cờ cảnh báo mục 8.2:** Có — công đoạn nào áp dụng Arduino (điểm mở #2) cần xác nhận để biết phạm vi trạm cần triển khai luồng này.

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-14

## Lịch sử triển khai (ghi chú backlog)


