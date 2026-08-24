# US-05b: Chọn & áp dụng kế hoạch cho công đoạn tại trạm (màn hình "Chọn kế hoạch")
**Là** Tổ trưởng
**Tôi muốn** chọn Công đoạn rồi chọn Kế hoạch tương ứng (xem lại thông tin + tiến độ), rồi bấm Áp dụng
**Để** đưa đúng kế hoạch (kể cả kế hoạch đang tạm dừng muốn chạy tiếp) vào chạy tại 1 công đoạn cụ thể của Line, không bị giới hạn bởi công đoạn vật lý của trạm đang thao tác — phục vụ Tổ trưởng quản lý nhiều công đoạn từ 1 vị trí

**Acceptance Criteria**
- **AC1 — Combobox Công đoạn không giới hạn theo trạm vật lý**
  - Given Tổ trưởng đang ở chế độ nâng quyền tại bất kỳ trạm nào thuộc Line Y
  - When mở màn hình "Chọn kế hoạch"
  - Then combobox Công đoạn liệt kê **mọi công đoạn đã cấu hình cho Line Y** (không chỉ công đoạn vật lý của trạm đang đứng), cho phép cấu hình kế hoạch cho công đoạn khác từ xa (mục 6 quy tắc 13)
  - **Bug phát hiện 17/08/2026** (cùng đợt rà soát gap "hiển thị Id thay vì tên Line" ở US-05 AC1a): `PlanSelectionPage.xaml` đang hiển thị dòng chữ **hardcode cứng** "Line 1 (theo trạm đang đăng nhập)..." thay vì bind đúng Tên Line thật của trạm — cần sửa thành bind động lấy từ Line thật của `StationOptions.LineId` (qua danh mục Line, hiển thị Tên, không phải số Id lẫn không phải chuỗi tĩnh sai)
- **AC2 — Danh sách kế hoạch lọc theo Line + Công đoạn đã chọn**
  - Given đã chọn Công đoạn X (thuộc Line Y)
  - When mở combobox "Kế hoạch sản xuất"
  - Then chỉ hiển thị các kế hoạch thuộc đúng Line Y (mọi kế hoạch của Line Y đều tự động áp dụng công đoạn X nếu X thuộc trình tự đã cấu hình cho Line Y — US-03); mặc định **ẩn** các kế hoạch đã `Completed`/`Cancelled` tại công đoạn X (US-05a AC7)
- **AC3 — Hiển thị tiến độ cho kế hoạch đang tạm dừng**
  - Given trong danh sách có 1 kế hoạch đang `Paused` tại công đoạn X
  - When hiển thị trong combobox/danh sách chọn
  - Then hiện rõ tiến độ dạng "Đã chạy 400/1000 — còn 600" (tính động, US-05a AC4), không hiển thị như kế hoạch còn nguyên số lượng gốc
- **AC4 — Chọn kế hoạch hiển thị lại thông tin ra textbox**
  - Given Tổ trưởng chọn 1 kế hoạch trong combobox
  - When hệ thống load thông tin
  - Then hiển thị (readonly) đầy đủ Khách hàng/Model/Lot/Revision/số lượng/takt time/thời gian bắt đầu/tên nhân viên tương ứng để xem lại trước khi áp dụng
- **AC5 — Áp dụng**
  - Given đã chọn đúng Công đoạn + Kế hoạch
  - When Tổ trưởng bấm "Áp dụng"
  - Then kế hoạch chuyển `Running` cho đúng (Line, Công đoạn) đó (US-05a AC1); màn hình chính của trạm tương ứng công đoạn đó cập nhật hiển thị đúng kế hoạch vừa áp dụng ngay lập tức (real-time nếu trạm đang mở sẵn)

**Nguồn FR:** FR-05a (phần UI màn hình chọn kế hoạch), mục 6 quy tắc 13
**Phụ thuộc:** US-05 (kế hoạch phải tồn tại), US-05a (trạng thái & tiến độ động), US-03 (trình tự công đoạn của Line — xác định công đoạn nào áp dụng cho mọi kế hoạch của Line đó)
**Cờ cảnh báo mục 8.2:** Không.
**UI:** `Station.Wpf` (chế độ Tổ trưởng đăng nhập nâng quyền tại trạm) — màn hình "Chọn kế hoạch", tách riêng khỏi màn hình "Cài đặt kế hoạch" (US-05).

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-17

## Lịch sử triển khai (ghi chú backlog)

Backend `GET api/v1/production-plan-stages?lineId=&stageId=&includeClosed=` (`ProductionPlanStageSelectionController`) + UI `Station.Wpf` (`PlanSelectionPage`/`PlanSelectionViewModel`) đầy đủ AC1-AC5: combobox "Công đoạn" liệt kê đủ mọi công đoạn trong trình tự của Line (US-03, qua `ILineStageSequenceApiClient.GetByLineAsync` join tên `IStageApiClient`), không giới hạn theo công đoạn vật lý của trạm; tên Line hiển thị đúng (bỏ hardcode); Áp dụng/Tạm dừng/Đóng gọi đúng `ProductionPlanStagesController`. Build sạch, 125/125 test Application pass. **17/08: người dùng đã tự chạy app, xác nhận trực quan hoạt động đúng**
