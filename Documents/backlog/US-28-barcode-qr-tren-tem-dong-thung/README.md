# US-28: Barcode/QR trên tem đóng thùng

**Là** Admin (thiết kế mẫu tem tại web-admin) và Operator/Tổ trưởng (in tem tại trạm Đóng thùng)
**Tôi muốn** tem dán thùng in ra có kèm mã vạch/QR chứa thông tin nhận diện thùng hàng
**Để** nhà kho/khách hàng quét được bằng máy quét khi nhập/xuất kho, thay vì phải đọc và nhập tay bằng mắt

**Bối cảnh:** phát sinh từ phân tích của `ba` ngày 26/08/2026 khi rà lại cơ chế merge dữ liệu vào template in tem (US-24/US-25) — cơ chế hiện tại (`PackingLabelGenerator.cs`) chỉ thay token `{{...}}` bằng CHỮ trong các ô Excel, hoàn toàn chưa có khả năng chèn ảnh mã vạch/QR. Người giao việc xác nhận có nhu cầu thật (26/08/2026), cần story riêng vì đây là mở rộng kỹ thuật không nhỏ (thêm thư viện sinh mã, quy ước token mới, chèn ảnh vào đúng vị trí cell).

**Acceptance Criteria** *(dự thảo — còn điểm mở, xem mục "Cờ cảnh báo" bên dưới, CẦN chốt với người giao việc trước khi dev implement)*

- **AC1 — Quy ước token ảnh mã vạch/QR trong template**
  - Given Admin đang thiết kế file mẫu tem `.xlsx` (US-24/AC4), đã quen thao tác gõ token chữ dạng `{{Model}}`...
  - When Admin muốn 1 ô nào đó hiển thị mã vạch/QR thay vì chữ
  - Then Admin gõ 1 token riêng theo đúng quy ước mới (ví dụ `{{BarcodeBoxCode}}` hoặc `{{QrBoxCode}}` — tên chính xác cần chốt), đặt vào đúng ô muốn hiển thị mã, giống hệt cách dùng token chữ hiện có — không cần công cụ thiết kế nào khác ngoài Excel
- **AC2 — Sinh mã và chèn vào đúng vị trí khi in tem**
  - Given tem đang được tạo cho 1 thùng cụ thể (US-25/AC4)
  - When hệ thống merge dữ liệu vào template và phát hiện ô chứa token ảnh mã vạch/QR
  - Then hệ thống sinh ảnh mã (barcode hoặc QR tuỳ token) mã hoá đúng nội dung đã chốt (xem điểm mở #1 bên dưới), chèn ảnh vào đúng vị trí/kích thước ô đó, xoá nội dung token chữ gốc
- **AC3 — Không có ô nào chứa token mã trong template**
  - Given file mẫu tem của Model không chứa token ảnh mã vạch/QR nào (Admin không dùng tính năng này)
  - When hệ thống merge dữ liệu
  - Then tem in ra như cũ (chỉ có chữ, không có mã) — không bắt buộc mọi Model phải có mã vạch/QR

**Nguồn FR:** chưa có trong SRS gốc — cần bổ sung FR mới nếu chốt triển khai (đặt tạm FR-28, chờ chốt)
**Phụ thuộc:** US-24 (cơ chế template + `IPackingLabelGenerator`), US-25 (luồng in tem tự động)
**Cờ cảnh báo mục 8.2 (ĐIỂM MỞ — bắt buộc hỏi lại người giao việc/PO trước khi dev implement, không tự suy luận):**
1. **Loại mã cụ thể:** barcode 1D (vd Code128) hay QR code, hay cả 2 tuỳ Model?
2. **Nội dung mã hoá:** chỉ `BoxNo`, hay cần ghép thêm `Model`/`Lot`/mã kế hoạch để nhận diện duy nhất khi quét (BoxNo một mình có thể trùng giữa các Lot/kế hoạch khác nhau)?
3. **Thư viện sinh mã:** dự án hiện chưa có dependency nào cho barcode/QR — cần chọn (vd `ZXing.Net`, `QRCoder`, `BarcodeLib`) và thêm qua đúng flow "thêm package" trong `CLAUDE.md` (`dotnet add ... package ... --version ...`)
4. **Máy in tại trạm Đóng thùng thực tế đã xác nhận độ phân giải in đủ để mã quét được chưa?** — cơ chế in hiện tại (US-25) dựa vào `Process.Start(Verb="print")` mở bằng Excel/LibreOffice, chưa test với máy in vật lý thật (ghi nhận sẵn trong US-25 lịch sử triển khai) — rủi ro này tồn tại độc lập với US-28 nhưng ảnh hưởng trực tiếp khả năng đọc được mã sau khi in

---

## Trạng thái triển khai

- **Trạng thái:** ⬜ Chưa làm
- **Cập nhật:** 2026-08-26

## Lịch sử triển khai (ghi chú backlog)

#### 2026-08-26 (ba, tạo story mới)
- Phát sinh từ phân tích cơ chế in tem đóng thùng (US-24/US-25) theo yêu cầu người giao việc — xem chi tiết phân tích gốc trong lịch sử hội thoại, tóm tắt bối cảnh đã ghi ở đầu file này.
- Người giao việc xác nhận CÓ nhu cầu barcode/QR (26/08/2026) nhưng CHƯA chốt chi tiết (loại mã, nội dung mã hoá, thư viện) — AC trên là dự thảo, cờ cảnh báo mục 8.2 liệt kê rõ các điểm cần hỏi lại trước khi bắt đầu code.
