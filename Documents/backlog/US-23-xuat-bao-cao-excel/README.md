# US-23: Xuất báo cáo Excel
**Là** Ban quản lý / Tổ trưởng
**Tôi muốn** xuất báo cáo tổng hợp và báo cáo tỷ lệ lỗi ra file Excel theo bộ lọc đã chọn
**Để** lưu trữ, chia sẻ hoặc xử lý số liệu ngoài hệ thống

**Acceptance Criteria**
- **AC1 — Xuất đúng dữ liệu theo bộ lọc** *(AC-22 gốc)*
  - Given người dùng đã chọn bộ lọc (Line, khoảng thời gian, công đoạn) trên màn hình báo cáo
  - When bấm xuất báo cáo
  - Then tải về file Excel (.xlsx) đúng dữ liệu đã lọc *(AC-22)*
- **AC2 — Áp dụng cho cả 2 loại báo cáo**
  - Given báo cáo tổng hợp (FR-21) hoặc báo cáo tỷ lệ lỗi (FR-20)
  - When người dùng chọn xuất Excel từ 1 trong 2 màn hình này
  - Then hệ thống xuất đúng loại báo cáo tương ứng ra .xlsx
- **AC3 — Chỉ hỗ trợ định dạng Excel**
  - Given yêu cầu xuất báo cáo ở giai đoạn này
  - When người dùng thao tác xuất
  - Then chỉ có tùy chọn .xlsx, không có tùy chọn PDF

**Nguồn FR:** FR-23
**Phụ thuộc:** US-21 (báo cáo tổng hợp), US-20 (báo cáo tỷ lệ lỗi)
**Cờ cảnh báo mục 8.2:** Có — **nội dung cụ thể cần có trong báo cáo Excel (các cột dữ liệu, cách nhóm/tổng hợp) chưa xác định (điểm mở #4)**. Dev cần hỏi lại stakeholder trước khi thiết kế mẫu file Excel; hiện chỉ biết được xuất từ 2 báo cáo FR-20/FR-21 theo bộ lọc Line/thời gian/công đoạn.

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần
- **Cập nhật:** 2026-08-19

## Lịch sử triển khai (ghi chú backlog)

**19/08/2026 (dev, phạm vi đã thu hẹp — chốt lại với Ban quản lý)**: CHỈ implement export Excel cho FR-21 (báo cáo "Theo Lot", `LotReportTab`) — KHÔNG làm FR-20 (báo cáo tỷ lệ lỗi, US-20 vẫn ⬜ Chưa làm nên chưa có gì để export) và KHÔNG làm nút xuất cho tab "Theo Line (PLAN/BALANCE)" (`LineReportTab`), để lại cho đợt sau. Backend: `ILotReportExportService`/`LotReportExportService` mới (`Services/Reports/`, dùng `ClosedXML.Excel.XLWorkbook`) — sinh file .xlsx gồm 2 sheet, TÁI DÙNG nguyên vẹn `ILotReportService.GetLotSummaryAsync` (Sheet 1 "Tổng hợp": Lot/Model/Khách hàng/Revision/Thời gian bắt đầu/Tổng số lượng Lot + bảng breakdown Line/Công đoạn/OK/NG/Đủ số lượng Lot?, đúng field `LotSummaryDto`/`LotStageRowDto` hiện có, không bịa thêm cột) và `IScanService.GetAllHistoryForLotAsync` (method MỚI, non-paginated — Sheet 2 "Chi tiết lượt scan": TagCode/Trạm thực hiện/Nhân viên/Thời điểm/Kết quả/Lý do lỗi/Người xác nhận + 4 field rework khi NG, gộp mọi Line/Công đoạn của Lot, KHÔNG giới hạn 200 dòng như UI). `ScanService` refactor: tách helper `BuildHistoryItemsAsync` dùng chung cho `GetHistoryAsync` (phân trang, UI) và `GetAllHistoryForLotAsync` (toàn bộ, export) — không đổi hành vi phân trang hiện có. Endpoint mới `GET api/v1/reports/lots/{lot}/export?from=&to=` (`LotReportsController`, cùng policy `Report.View`, trả `FileContentResult` Content-Type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`) — trả 404 khi Lot không tồn tại, giống `GetSummary`. `web-admin`: `LotReportTab.tsx` thêm nút "Xuất Excel" (`extra` của Card tổng quan, chỉ hiện khi đã có `LotSummaryDto`), gọi `exportLotReport` (`api/lotReportsApi.ts`, `responseType: 'blob'`) rồi tải file `bao-cao-lot-{lot}.xlsx` qua anchor tag + `URL.createObjectURL`. Build: `dotnet build ProductionMES.sln -c Release` sạch 0 lỗi/0 warning (Debug bị lock file bởi `ProductionMES.Api`/Visual Studio đang chạy sẵn — không phải lỗi biên dịch, đã xác nhận qua build Release + build riêng từng project Debug không lỗi CS). Test: `dotnet test tests/ProductionMES.Application.Tests` 261/261 pass (mới: `LotReportExportServiceTests` 5 test — Lot không tồn tại trả null/không gọi tiếp lấy lịch sử scan, đủ 2 sheet đúng tên, Sheet 1 đúng dữ liệu tổng quan+breakdown, "Chưa xác định" khi LotTotalQuantity/FirstScannedAtUtc null, Sheet 2 lấy đủ >200 bản ghi không phân trang + đúng field rework khi NG; 3 test mới trong `ScanServiceHistoryTests` cho `GetAllHistoryForLotAsync` — không phân trang, lọc theo khoảng thời gian, gồm cả kết quả bị từ chối tự động không chỉ Ok/Ng). `web-admin`: `npm run lint` (oxlint) sạch, `npm run build` (tsc -b && vite build) pass. **Giữ 🟡 Một phần** — CHƯA xác nhận trực quan (không có công cụ chạy `npm run dev` + click UI + mở file .xlsx thật trong phiên này, cần người tự tải file và mở bằng Excel để xác nhận đúng 2 sheet/định dạng); CHƯA làm FR-20 (US-20 chưa implement) và tab "Theo Line" (đã xác nhận rõ với Ban quản lý là ngoài phạm vi đợt này). Code CHƯA commit.
