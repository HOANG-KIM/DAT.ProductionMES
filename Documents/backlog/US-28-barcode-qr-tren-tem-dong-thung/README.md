# US-28: Barcode/QR trên tem đóng thùng

**Là** Admin (thiết kế mẫu tem tại web-admin) và Operator/Tổ trưởng (in tem tại trạm Đóng thùng)
**Tôi muốn** tem dán thùng in ra có kèm mã QR chứa thông tin nhận diện thùng hàng, và in được ổn định trên máy trạm dù cài Microsoft Excel hay LibreOffice
**Để** nhà kho/khách hàng quét được bằng máy quét khi nhập/xuất kho, thay vì phải đọc và nhập tay bằng mắt

**Bối cảnh:** phát sinh từ phân tích của `ba` ngày 26/08/2026 khi rà lại cơ chế merge dữ liệu vào template in tem (US-24/US-25) — cơ chế cũ (`PackingLabelGenerator.cs`) chỉ thay token `{{...}}` bằng CHỮ trong các ô Excel, chưa chèn được ảnh mã QR. Ngày 09/10/2026 người giao việc chốt các điểm mở, lấy phần mềm in tem cũ **PrintLabel** (WinForms, `E:\1. C#\1. Winform\PrintLabelPacking\PrintLabel\PrintLabel`, đã chạy thực tế ở xưởng) làm tham chiếu cho nội dung QR và cách in.

**Acceptance Criteria** *(chốt 09/10/2026)*

- **AC1 — Token ảnh QR trong template**
  - Given Admin đang thiết kế file mẫu tem `.xlsx` (US-24/AC4)
  - When Admin muốn 1 vùng trên tem hiển thị mã QR
  - Then Admin gõ token `{{QrCode}}` vào ô đó (hoặc ô đầu của vùng ô gộp) — giống hệt cách dùng token chữ, không cần công cụ thiết kế nào khác ngoài Excel
- **AC2 — Sinh QR và chèn vào đúng vị trí khi in tem**
  - Given tem đang được tạo cho 1 thùng cụ thể (US-25/AC4)
  - When hệ thống merge dữ liệu vào template và gặp ô chứa `{{QrCode}}`
  - Then hệ thống sinh ảnh QR (ECC M, UTF-8) mã hoá `Model\Lot\Số thùng\Nhân viên vận hành\Số lượng/thùng\yyyyMMdd` (đúng định dạng tem cũ của PrintLabel, để máy quét/kho đang đọc tem cũ không phải đổi), chèn ảnh vuông căn giữa trong ô (hoặc cả vùng ô gộp) đó, co vừa kích thước vùng, xoá token chữ gốc
- **AC3 — Template không có `{{QrCode}}`**
  - Given file mẫu tem của Model không chứa `{{QrCode}}`
  - When hệ thống merge dữ liệu
  - Then tem in ra như cũ (chỉ có chữ) — không bắt buộc mọi Model phải có QR
- **AC4 — Token chữ bổ sung**
  - Given Admin cần in thêm thông tin của kế hoạch sản xuất lên tem
  - When Admin dùng các token `{{Customer}}`, `{{Lot}}`, `{{OperatorNames}}` (Nhân viên vận hành, thay ô "NV cân" của tem cũ), `{{PackedDate}}` (ngày đóng thùng `dd/MM/yyyy`, không kèm giờ)
  - Then hệ thống thay đúng giá trị lấy từ kế hoạch sản xuất của thùng; danh sách token tham chiếu + cảnh báo token sai (US-24/AC10) tự cập nhật theo
- **AC5 — In tem bằng Excel hoặc LibreOffice, chọn theo cấu hình trạm**
  - Given trạm Đóng thùng có cài Microsoft Excel và/hoặc LibreOffice
  - When hệ thống in tem (tự động khi đủ thùng, hoặc "In lại")
  - Then trạm in im lặng (không mở cửa sổ) theo `LabelPrint:Engine` trong `appsettings.json` của trạm: `Auto` (Excel nếu có, không thì LibreOffice — mặc định), `Excel`, hoặc `LibreOffice`; luôn in ra **máy in mặc định của Windows** (chọn máy in tem bằng cách đặt làm mặc định trên máy trạm). Thiếu phần mềm/quá `LabelPrint:TimeoutSeconds` → báo lỗi rõ ràng cạnh bộ đếm thùng, vẫn không chặn đóng thùng kế tiếp và vẫn có "In lại" (giữ nguyên US-25/AC13)

**Quyết định đã chốt (09/10/2026, người giao việc):**
1. **Template theo khách hàng:** mỗi Model chỉ thuộc 1 khách hàng → giữ mô hình "1 Model = 1 template" của US-24, KHÔNG cần thư viện template riêng.
2. **Loại mã:** QR (không làm barcode 1D). Thư viện `QRCoder` 1.6.0 (cùng thư viện PrintLabel dùng).
3. **"Ecode"/"NV cân" của tem cũ:** = `ProductionPlan.OperatorNames` (Nhân viên vận hành Tổ trưởng nhập ở Cài đặt kế hoạch). Lưu ý: là free-text cấp kế hoạch — nhập nhiều tên thì tem in cả danh sách.
4. **Cách in:** hỗ trợ cả Excel và LibreOffice, chọn bằng cấu hình cục bộ tại trạm (không đưa lên web-admin — phụ thuộc phần mềm/máy in từng máy, cùng nguyên tắc CLAUDE.md mục "Cấu hình").
5. **Máy in:** luôn in ra máy in mặc định của Windows, KHÔNG cấu hình tên máy in (chốt 09/10/2026).
6. **Số thùng trên tem:** template mẫu có ô riêng hiển thị số thùng (`{{BoxNo}}`), giống tem cũ của PrintLabel.

**Nguồn FR:** chưa có trong SRS gốc — cần bổ sung FR-28 vào SRS
**Phụ thuộc:** US-24 (cơ chế template + `IPackingLabelGenerator`), US-25 (luồng in tem tự động)

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần
- **Cập nhật:** 2026-10-09

## Lịch sử triển khai (ghi chú backlog)

#### 2026-10-09 (dev, chỉ in máy in mặc định + thêm số thùng vào Template_v1)
- **Station.Wpf:** bỏ `LabelPrint:PrinterName` theo quyết định người giao việc — `ExcelComLabelPrinter` bỏ dò `ActivePrinter`/cổng `NeXX`, `LibreOfficeLabelPrinter` bỏ `--pt` (chỉ còn `-p`), `ILabelPrinter.PrintAsync` bỏ tham số `printerName`; luôn in ra máy in mặc định của Windows.
- **Template:** `templates/Template_v1.xlsx` sửa về đúng bố cục tem cũ của PrintLabel — vùng QR gộp `A1:A5` (trước đó gộp nhầm `A1:A6` nên mất chỗ ghi số thùng), `A6` = `{{BoxNo}}` (chữ đậm căn giữa, có viền). Đã xuất PDF bằng Excel kiểm tra: QR căn giữa `A1:A5`, số thùng hiện ở `A6`.
- **Build:** `dotnet build ProductionMES.sln` 0 Warning/0 Error. Không đổi Application nên test giữ 365/365.
- **Còn thiếu / lý do giữ 🟡:** như đợt trước — chưa in thử trên máy in tem thật.

#### 2026-10-09 (dev, implement AC1-AC5 — QR + token mới + in Excel/LibreOffice)
- **Application:**
  - Thêm package `QRCoder` 1.6.0 (`dotnet add ... --version 1.6.0`).
  - `PackingLabelTokens`: thêm `{{PackedDate}}`, `{{Customer}}`, `{{Lot}}`, `{{OperatorNames}}`, `{{QrCode}}` (hằng `QrCodeToken`) — tổng 14 token.
  - `PackingLabelGenerator`: token ảnh `{{QrCode}}` → `PngByteQRCode` (ECC M, `forceUtf8`), chèn bằng ClosedXML, ảnh vuông căn giữa vùng ô/ô gộp; ô neo được dời tới đúng hàng/cột chứa điểm bắt đầu (Excel cắt offset vượt kích thước ô neo). `BuildQrContent` public để test định dạng.
  - `PackingLabelData` + `PackingBoxService.GenerateLabelAsync`: đọc `Customer`/`Lot`/`OperatorNames` từ `ProductionPlan` lúc tạo tem (không snapshot — `OperatorNames` sửa được nên "In lại" thùng cũ sau khi đổi tên sẽ in tên MỚI).
- **Station.Wpf:**
  - `LabelPrintOptions` (section `LabelPrint`: `Engine`/`PrinterName`/`LibreOfficePath`/`TimeoutSeconds`) gắn vào `StationOptions`, mẫu trong `appsettings.json`.
  - `Services/PackingBoxes/Printing/`: `ILabelPrinter`, `ExcelComLabelPrinter` (COM late-binding trên luồng STA riêng, `PrintOut()`, giải phóng COM — theo `PrintService.cs` của PrintLabel; tên máy in thử trần rồi `"... on NeXX:"`), `LibreOfficeLabelPrinter` (`soffice --headless -p`/`--pt`, profile riêng `%LOCALAPPDATA%\ProductionMES\libreoffice-profile` để không dính instance LibreOffice đang mở).
  - `PackingLabelPrintService`: bỏ ShellExecute `Verb="print"`, chọn printer theo `Engine` (Auto = Excel → LibreOffice), in tuần tự (`SemaphoreSlim`), timeout, xoá file tạm sau khi in; mọi lỗi bọc `PackingLabelPrintException` (giữ nguyên hành vi AC13 của US-25 ở `AndonBoardViewModel`).
- **Template:** `templates/Template_v1.xlsx` (`A1:A6` = `{{QrCode}}`, `D6` = `NV cân: {{OperatorNames}}`) và `templates/Template_selims.xlsx` (chuyển từ PrintLabel sang token: `A1:A5` = `{{QrCode}}`, `C2` = `{{Model}}`, `C3` = `{{PackingQuantity}} EA/BOX ({{GrossWeight}} Kg)`, `C4` = `{{PackedDate}}`, `A6` = `{{BoxNo}}`; Description/Customer giữ chữ tĩnh như tem cũ).
- **Test:** `PackingLabelGeneratorTests` (6 test, dùng file .xlsx thật: token chữ mới, chèn QR đúng ô, QR theo vùng gộp, không có QR, định dạng nội dung QR, file hỏng) + 1 test `PackingBoxServiceTests` (Customer/Lot/OperatorNames từ kế hoạch). `dotnet test`: 365/365 pass.
- **Build:** `dotnet build ProductionMES.sln` 0 Warning/0 Error. web-admin không đổi code (danh sách token lấy từ API).
- **Kiểm tra thủ công:** merge dữ liệu mẫu vào 2 template rồi xuất PDF bằng cả Excel (`ExportAsFixedFormat`) và LibreOffice (`--convert-to pdf`) — token thay đúng, tiếng Việt có dấu đúng, QR căn giữa vùng `A1:A6`/`A1:A5`.
- **Còn thiếu / lý do giữ 🟡:**
  - Chưa in thật ra máy in tem (Zebra GT800) bằng `ExcelComLabelPrinter`/`LibreOfficeLabelPrinter` — cần chạy Station.Wpf tại trạm thật, in 1 tem mỗi cách, quét thử QR bằng máy quét của kho.
  - LibreOffice không nhận khổ giấy tuỳ chỉnh của template (paper size 257) — khi xuất PDF ra khổ A4; cần xác nhận khi in thật trên máy in tem có đúng khổ nhãn không, nếu lệch thì ưu tiên `Engine = Excel` ở trạm đó.
  - Chưa bổ sung FR-28 vào SRS.

#### 2026-08-26 (ba, tạo story mới)
- Phát sinh từ phân tích cơ chế in tem đóng thùng (US-24/US-25) theo yêu cầu người giao việc — xem chi tiết phân tích gốc trong lịch sử hội thoại, tóm tắt bối cảnh đã ghi ở đầu file này.
- Người giao việc xác nhận CÓ nhu cầu barcode/QR (26/08/2026) nhưng CHƯA chốt chi tiết (loại mã, nội dung mã hoá, thư viện) — AC trên là dự thảo, cờ cảnh báo mục 8.2 liệt kê rõ các điểm cần hỏi lại trước khi bắt đầu code.
