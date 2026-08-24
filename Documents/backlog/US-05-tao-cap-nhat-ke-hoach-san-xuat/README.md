# US-05: Tạo/cập nhật kế hoạch sản xuất (màn hình "Cài đặt kế hoạch")
**Là** Tổ trưởng / Quản lý chuyền
**Tôi muốn** nhập Khách hàng, Model, Lot, Revision, số lượng lot, takt time, thời gian bắt đầu, tên nhân viên vận hành và lưu thành 1 kế hoạch
**Để** có bản ghi kế hoạch làm cơ sở áp dụng vào từng công đoạn của Line khi đối chiếu sản lượng thực tế lúc scan tem

**Acceptance Criteria**
- **AC1 — Tạo kế hoạch mới**
  - Given Line đã tồn tại và đang hoạt động
  - When Tổ trưởng nhập đầy đủ: Line áp dụng, Khách hàng, Model, Lot, số lượng kế hoạch (theo Lot), takt time, thời gian bắt đầu (ngày + giờ), tên nhân viên vận hành tại trạm/công đoạn phụ trách lô này (có thể nhiều người), và lưu
  - Then kế hoạch được tạo ở trạng thái `Draft` (chưa chạy công đoạn nào — xem US-05a để áp dụng vào công đoạn cụ thể)
- **AC1a — Chọn Line từ danh mục, không gõ tay Id** *(bổ sung 17/08/2026 — sửa gap UI phát hiện: màn hình đang để gõ tay số Id)*
  - Given danh mục Line (US-01) đã có ít nhất 1 Line đang hoạt động (`IsActive = true`)
  - When Tổ trưởng mở màn hình "Cài đặt kế hoạch" để tạo/sửa kế hoạch
  - Then ô "Line áp dụng" hiển thị dạng combobox liệt kê Line theo **Tên**, chỉ hiển thị các Line đang hoạt động (giống cách chọn Công đoạn ở US-05b AC1) — không có ô nhập tay số Id
- **AC1b — Line đã vô hiệu hóa vẫn hiển thị đúng tên cho kế hoạch cũ**
  - Given 1 kế hoạch cũ đang gán cho Line đã bị vô hiệu hóa (US-01 AC3) sau khi kế hoạch được tạo
  - When Tổ trưởng mở lại kế hoạch đó để xem/sửa
  - Then combobox vẫn hiển thị đúng Tên Line đó (dù không còn active) để không gây hiểu nhầm dữ liệu bị mất, nhưng Line này không xuất hiện trong danh sách chọn cho kế hoạch MỚI
- **AC1c — Danh sách kế hoạch hiển thị tên Line**
  - Given danh sách kế hoạch đang hiển thị trên màn hình "Cài đặt kế hoạch"
  - When Tổ trưởng xem cột "Line"
  - Then hiển thị **Tên Line**, không hiển thị số Id thô
- **AC1d — Nhập thời gian bắt đầu đủ ngày + giờ:phút** *(bổ sung 17/08/2026 — sửa gap UI phát hiện: chỉ có DatePicker chọn ngày, thiếu ô giờ)*
  - Given Tổ trưởng đang tạo/sửa kế hoạch
  - When nhập "Thời gian bắt đầu"
  - Then màn hình có cả ô chọn Ngày và ô nhập Giờ:Phút (định dạng 24h, "HH:mm"); giá trị gửi lên API là DateTime đầy đủ ngày + giờ + phút đã nhập (không mặc định 00:00 nếu đã chọn giờ khác)
  - Danh sách kế hoạch (DataGrid) và màn hình "Chọn kế hoạch" (US-05b AC4) hiển thị lại đúng "dd/MM/yyyy HH:mm" đã nhập, không phải định dạng .NET mặc định
  - **Ngoại lệ khi xoá trắng** *(bổ sung 17/08/2026, vòng 2)*: riêng ô "Giờ:Phút" (`StartTimeOfDay`) — nếu Tổ trưởng xoá trắng nội dung rồi rời khỏi ô (mất focus) mà không nhập lại, hệ thống tự hiển thị lại "00:00" thay vì để trống. Ngoại lệ này CHỈ áp dụng cho trường hợp rỗng lúc mất focus; các trường hợp nhập sai định dạng khác (vd "25:99") vẫn giữ nguyên hành vi ở trên (từ chối lưu, báo lỗi, không tự sửa ngầm). Trong lúc ô còn đang giữ focus (đang gõ dở), chuỗi rỗng tạm thời vẫn được phép, không bị ép ngay — tránh phá luồng gõ khi Tổ trưởng bôi đen để nhập giá trị mới.
- **AC1e — Nhập/hiển thị Takt time theo định dạng phút:giây, tự quy đổi ra giây khi tính toán** *(bổ sung 17/08/2026 — sửa gap UI phát hiện: đang nhập/hiển thị số giây thô)*
  - Given Tổ trưởng đang tạo/sửa kế hoạch
  - When nhập ô "Takt time" theo định dạng mm:ss (vd "1:30" cho 90 giây, giây luôn 2 chữ số 00-59, chỉ số nguyên giây — không cho phần thập phân trên UI)
  - Then hệ thống tự quy đổi sang số giây nguyên để tính sản lượng chuẩn/giờ (AC3, FR-06) và gửi lên API dưới dạng giây; nếu nhập giây > 59 hoặc ký tự không hợp lệ, hệ thống từ chối lưu và báo lỗi rõ ràng, không tự sửa ngầm
  - Danh sách kế hoạch (DataGrid, cột "Takt") và màn hình "Chọn kế hoạch" (US-05b AC4) hiển thị lại đúng định dạng mm:ss, không hiển thị số giây thô
  - **Ngoại lệ khi xoá trắng** *(bổ sung 17/08/2026, vòng 2)*: riêng ô "Takt time" — nếu Tổ trưởng xoá trắng nội dung rồi rời khỏi ô (mất focus) mà không nhập lại, hệ thống tự hiển thị lại "0:00" (0 phút 00 giây, đúng định dạng mm:ss chuẩn của hệ thống) thay vì để trống. Ngoại lệ này CHỈ áp dụng cho trường hợp rỗng lúc mất focus; các trường hợp nhập sai định dạng khác (vd giây > 59, ký tự không hợp lệ) vẫn giữ nguyên hành vi ở trên (từ chối lưu, báo lỗi, không tự sửa ngầm). Trong lúc ô còn đang giữ focus (đang gõ dở), chuỗi rỗng tạm thời vẫn được phép, không bị ép ngay.
- **AC2 — Revision được để trống**
  - Given Tổ trưởng đang nhập kế hoạch mới
  - When để trống ô Revision
  - Then hệ thống vẫn cho lưu bình thường, không bắt buộc
- **AC3 — Hiển thị ngay sản lượng chuẩn/giờ khi nhập takt time**
  - Given Tổ trưởng đang nhập takt time
  - When gõ giá trị takt time
  - Then hệ thống tính và hiển thị ngay sản lượng chuẩn/giờ (`3600 / Takt time`, FR-06) cạnh ô nhập, giúp phát hiện sớm lỗi nhập liệu trước khi lưu
- **AC4 — Cập nhật kế hoạch chưa từng chạy (`Draft`)**
  - Given kế hoạch đang ở trạng thái `Draft`, chưa `Running` ở công đoạn nào
  - When Tổ trưởng chỉnh sửa bất kỳ trường nào (kể cả số lượng, takt time)
  - Then thông tin được cập nhật tự do, không ràng buộc gì thêm
- **AC5 — Cập nhật kế hoạch đã có công đoạn đang `Running`/`Paused`**
  - Given kế hoạch đã có ít nhất 1 công đoạn đang `Running` hoặc `Paused` (đã có lượt scan OK, xem US-05a)
  - When Tổ trưởng chỉnh sửa số lượng kế hoạch hoặc takt time
  - Then hệ thống cảnh báo rõ đây là kế hoạch đang chạy dở, xác nhận trước khi lưu — tránh sửa nhầm làm sai lệch cách tính "còn lại" (US-05a AC4) hoặc chỉ số +/- đang hiển thị tại trạm
  - **Lưu ý phạm vi áp dụng**: AC này **chỉ áp dụng cho Số lượng kế hoạch/Takt time** — Khách hàng/Model/Lot/Revision theo quy tắc khác hẳn (khóa tuyệt đối, không có Confirm), xem AC6 ngay dưới đây; 2 rule không gộp chung điều kiện kích hoạt.
- **AC6 — Khóa tuyệt đối Khách hàng/Model/Lot/Revision khi kế hoạch đã có scan**
  - Given kế hoạch đã có **ít nhất 1 bản ghi lượt scan** (bất kể kết quả OK/NG/lỗi, bất kể `PlanStatus` của công đoạn đó là `Running`/`Paused`/`Completed`/`Cancelled` — không giới hạn như AC5 chỉ xét `Running`/`Paused`)
  - When Tổ trưởng cố sửa Khách hàng, Model, Lot, hoặc Revision (dù có gửi Confirm = true hay không)
  - Then hệ thống **từ chối tuyệt đối**, không có bất kỳ đường Confirm nào để ghi đè — thông báo rõ lý do (đã có lịch sử scan gắn với các giá trị hiện tại) và gợi ý tạo kế hoạch mới nếu nhập sai
- **AC7 — Nhập "Tổng số lượng Lot" (mới 19/08/2026, US-21a)**
  - Given Tổ trưởng/Admin đang tạo kế hoạch cho 1 Lot HOÀN TOÀN MỚI (chưa từng có `ProductionPlan` nào trước đó)
  - When lưu kế hoạch
  - Then **bắt buộc** nhập "Tổng số lượng Lot" — không cho lưu nếu bỏ trống; hệ thống lưu vào entity `Lot` dùng chung (khóa theo `Code` = giá trị Lot). Nếu Lot đã tồn tại từ trước (đã có `Lot.TotalQuantity`), giá trị hiển thị lại tự động, không bắt nhập lại nhưng vẫn sửa được tự do — áp dụng ngay cho MỌI kế hoạch khác cùng Lot (không giới hạn ở kế hoạch đang sửa) — xem US-21a AC1/AC2
- **AC8 — Cảnh báo khi "Tổng số lượng Lot" bị sửa nhỏ hơn số đã chạy thực tế**
  - Given Lot đang sửa đã có ≥1 dòng (Line, Công đoạn) với số lượng OK lớn hơn giá trị mới định nhập
  - When Tổ trưởng cố lưu
  - Then hệ thống cảnh báo, yêu cầu xác nhận trước khi lưu (soft-confirm, không hard-block) — xem US-21a AC3
- **AC9 — Gợi ý tiến độ Lot đã tồn tại khi tạo kế hoạch mới**
  - Given Tổ trưởng gõ/chọn 1 giá trị Lot đã có ≥1 `ProductionPlan` trước đó (bất kỳ Line/trạng thái)
  - When hệ thống nhận diện Lot đã tồn tại
  - Then hiển thị "Tổng SL Lot" hiện có (hoặc "Chưa xác định") + breakdown "đã chạy OK theo từng (Line, Công đoạn)" của Lot đó, hỗ trợ nhập `PlannedQuantity` chính xác cho kế hoạch mới — xem US-21a AC4

**Nguồn FR:** FR-05, mục 6 quy tắc 14 (AC6), FR-21a (AC7-AC9)
**Phụ thuộc:** US-01 (Line). AC6 (khóa tuyệt đối khi đã có scan) cần entity `Scan` đã tồn tại (US-07/US-08, đã xong — `ceb0ee1`) để kiểm tra "đã có ≥1 bản ghi scan"; nên triển khai AC6 cùng đợt với US-10 AC4 vì cả 2 cùng chạm `ProductionPlanService`/entity `Scan`. AC7-AC9 (mới 19/08/2026) cần entity `Lot` mới và `ILotService` (xem US-21a) — nên triển khai CÙNG ĐỢT với US-21a vì cả 2 cùng chạm entity `Lot` mới/màn hình `PlanSettingsPage`.
**Cờ cảnh báo mục 8.2:** Không trực tiếp.
**UI:** `Station.Wpf` (chế độ Tổ trưởng đăng nhập nâng quyền tại trạm) — KHÔNG phải `web-admin`, xem ADR-002 mục "Cập nhật phạm vi" (12/08/2026).
**Ghi chú:** Cập nhật 13/08/2026 theo FR-05 mới (bổ sung Khách hàng/Model/Lot/Revision, bỏ "ca làm việc", đổi nghĩa "tên nhân viên" thành người vận hành chứ không phải người đăng nhập thao tác màn hình). Phần ràng buộc "1 kế hoạch active" và vòng đời trạng thái đã tách hẳn sang **US-05a** (trước đây là AC2 của story này) vì trạng thái nay gắn cấp (Kế hoạch, Công đoạn), không phải cấp Kế hoạch. **Cập nhật 14/08/2026**: bổ sung AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi đã có scan) — gap phát hiện khi rà soát US-10 (lịch sử scan tra cứu qua join động, dễ hiển thị sai nếu kế hoạch bị sửa sau khi đã scan). AC6 **nên triển khai cùng đợt** với US-10 AC4 (snapshot 6 field vào `Scan`) vì cả 2 cùng xử lý 1 gap, cùng chạm entity `Scan`/`ProductionPlanService` — xem SRS mục 6 quy tắc 14, mục 8.1. **Cập nhật 19/08/2026 (BA)**: bổ sung AC7-AC9 ("Tổng số lượng Lot" nhập tay) theo phản hồi trực tiếp Ban quản lý về US-21a — xem chi tiết đầy đủ tại mục US-21a. **Cập nhật 19/08/2026 (dev, implement AC7-AC9)**: đã code xong cả backend (`ProductionPlanService`) lẫn UI `Station.Wpf` (`PlanSettingsPage`/`PlanSettingsViewModel`) — xem quyết định kỹ thuật đầy đủ (tên field, vị trí `ILotService`, cách tái dùng UX Confirm/endpoint Lot report) tại Ghi chú "19/08/2026, dev" ở mục US-21a (cả 2 story chạm chung entity `Lot` nên gộp ghi chú kỹ thuật vào 1 chỗ, tránh trùng lặp).

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-19

## Lịch sử triển khai (ghi chú backlog)

AC1-AC6 đã Xong trước đây (Backend `0c5b944`, AC6 `9f0e299`; UI `Station.Wpf` `PlanSettingsPage`/`PlanSettingsViewModel` đầy đủ AC1-AC6/AC1a-AC1e — người dùng đã xác nhận trực quan 17/08/2026). **19/08/2026 (dev, implement AC7-AC9)**: đã code xong đầy đủ AC7-AC9 theo entity `Lot` mới (xem US-21a) — `ProductionPlanService.CreateAsync`/`UpdateAsync` bắt buộc "Tổng số lượng Lot" khi Lot hoàn toàn mới (qua `ILotService.HasAnyProductionPlanAsync`, 409 nếu thiếu), soft-confirm khi giảm dưới thực tế đã chạy (tái dùng field `Confirm` sẵn có, cùng cơ chế 409-retry AC5); UI `Station.Wpf` `PlanSettingsPage`/`PlanSettingsViewModel` thêm ô nhập "Tổng số lượng Lot" + hint bắt buộc khi Lot mới + breakdown "đã chạy OK" khi gõ/chọn Lot đã tồn tại (LostFocus ô Lot gọi `ILotReportApiClient.GetSummaryAsync`, tái dùng `GET api/v1/reports/lots/{lot}`, không tạo API mới). Build `dotnet build ProductionMES.sln` sạch 0 `error CS` (chỉ còn lock MSB3021/MSB3027 do `ProductionMES.Api.exe` đang chạy sẵn từ VS — không phải lỗi code); `dotnet test tests/ProductionMES.Application.Tests` 247/247 pass. Migration `AddLot` đã tạo và **áp dụng thành công vào MySQL cục bộ** (`dotnet ef database update`, xác nhận qua `dotnet ef migrations list`). **19/08/2026 (người dùng xác nhận trực quan trên UI `Station.Wpf` thật)**: đã test OK ô nhập/hint/breakdown "Tổng số lượng Lot" hoạt động đúng; nhãn hiển thị đổi ngắn gọn "Tổng SL Lot" → "SL Lot" (`AndonBoardWindow.xaml`, `PlanSelectionPage.xaml`, `ProductionPlanStageSelectionDto.LotTotalQuantityDisplay`). Chuyển ✅ Xong.
