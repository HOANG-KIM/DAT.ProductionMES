# BACKLOG USER STORY — Hệ thống Quản lý Kế hoạch Sản xuất (DAT.ProductionMES)

**Nguồn căn cứ:** `Documents/SRS-he-thong-quan-ly-ke-hoach-san-xuat.md` (FR-01 → FR-23, mục 6 quy tắc chốt, mục 7 AC, mục 8.2 điểm còn mở), `Documents/ADR-001-lua-chon-wpf-hay-winforms.md`.
**Ngày lập:** 11/08/2026
**Cập nhật:** 13/08/2026 — (1) bổ sung AC cho US-09 để đồng bộ với FR-09a (khung giờ nghỉ theo Line) được thêm vào SRS ngày 13/08/2026, sau thời điểm backlog được lập lần đầu; (2) tách 2 khoảng trống phát sinh sau khi story gốc đã code xong thành story riêng — **US-01a** (khung giờ nghỉ theo Line, do FR-01/FR-09a bổ sung sau khi US-01 code xong) và **US-04a** (API Key theo trạm, do ADR-005 chốt sau khi US-04 code xong) — cả 2 đều là điều kiện tiên quyết bắt buộc trước khi triển khai US-07/US-08/US-09, đã cập nhật vào lộ trình triển khai đề xuất; (3) cập nhật **US-05** theo FR-05 mới (Khách hàng/Model/Lot/Revision, bỏ ca làm việc) và bổ sung 2 story mới — **US-05a** (vòng đời trạng thái kế hoạch `Draft/Running/Paused/Completed/Cancelled` theo từng cặp (Line, Công đoạn), tạm dừng/chạy lại/đóng độc lập, tính tiến độ động — FR-05a) và **US-05b** (màn hình "Chọn kế hoạch": chọn Công đoạn + Kế hoạch, hiển thị tiến độ, Áp dụng) — phát sinh từ phân tích UI kế hoạch sản xuất ngày 13/08/2026, đã cập nhật vào lộ trình triển khai đề xuất. **Cập nhật 14/08/2026** — (4) bổ sung US-05/AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi kế hoạch đã có scan) và US-10/AC4 (snapshot bất biến 6 field trong lịch sử scan) — xử lý gap phát hiện khi rà soát US-10: sửa kế hoạch sau khi đã scan làm lịch sử scan cũ hiển thị sai nếu chỉ tra cứu qua join động tới `ProductionPlan` hiện tại; đã đồng bộ vào SRS mục 6 quy tắc 14, mục 8.1.
**Ghi chú chung:** Backlog này là đầu vào để dev implement dần theo thứ tự đề xuất ở cuối tài liệu. Xem tiến độ thực tế ở bảng "TRẠNG THÁI TRIỂN KHAI" ngay bên dưới.

---

## TRẠNG THÁI TRIỂN KHAI

Quy ước cập nhật bảng này nằm ở `CLAUDE.md` (mục "Theo dõi tiến độ backlog") — agent `ba`/`dev` đọc trước khi làm việc và PHẢI tự cập nhật dòng tương ứng khi xong việc, không chờ người khác cập nhật hộ.

**Chú giải trạng thái:** ⬜ Chưa làm · 🔵 Đang làm · 🟡 Một phần (xem Ghi chú) · ✅ Xong

| US-ID | Tên | Trạng thái | Ghi chú | Cập nhật |
|---|---|---|---|---|
| US-01 | Quản lý danh mục Line | ✅ Xong | Backend + UI web-admin (`a42c9f2`, `d4dd6ab`) | 2026-08-14 |
| US-01a | Khung giờ nghỉ theo Line | ✅ Xong | `4f0a3ed` | 2026-08-14 |
| US-02 | Quản lý danh mục Công đoạn | ✅ Xong | Backend + UI web-admin (`a42c9f2`, `cedf66b`) | 2026-08-14 |
| US-03 | Cấu hình trình tự công đoạn cho Line | ✅ Xong | **17/08 (xác nhận trực quan)**: người dùng đã tự chạy `Station.Wpf`, thử đầy đủ thao tác thêm/gỡ/sắp xếp trình tự công đoạn của Line tại `LineStageSequencePage` — hoạt động đúng như AC1-AC8. **17/08 (implement lại theo thiết kế mới)**: entity `LineStageSequence` (LineId/StageId/SequenceNumber, unique theo LineId+SequenceNumber và LineId+StageId) thay hẳn `ProductionPlanStage.SequenceNumber` (đã xoá property này khỏi entity). `LineStageSequenceService`/`ILineStageSequenceService` (AddAsync/RemoveAsync/ReorderAsync/GetByLineAsync) + `LineStageSequencesController` (`api/v1/lines/{lineId}/stage-sequence`, dùng lại permission `ProductionPlanStage.*`) cover đủ AC1-AC7. `ProductionPlanStageService` sửa lại cơ chế "lazy get-or-create": `GetByProductionPlanAsync`/`ApplyAsync`/`PauseAsync`/`CloseAsync` tự tạo bản ghi `PlanStatus=Draft` khi cần dựa theo trình tự Line, `GetByLineAndStageAsync` (US-05b) liệt kê mọi `ProductionPlan` của Line thay vì chỉ các row đã tồn tại; `AddAsync/RemoveAsync/ReorderAsync` cũ đã xoá khỏi service/controller này (chuyển hẳn sang `LineStageSequenceService`) — `IProductionPlanStageService.GetByProductionPlanAsync`/`ProductionPlanStageDto` giữ nguyên shape nên `ScanService`/`ScanServiceTests` KHÔNG phải sửa. UI `Station.Wpf`: bỏ khu vực "Công đoạn của kế hoạch" khỏi `PlanSettingsPage`/`PlanSettingsViewModel`, thêm màn mới `LineStageSequencePage`/`LineStageSequenceViewModel` (+ API client `ILineStageSequenceApiClient`), điều hướng từ `HomePage`/`MainWindow` theo ADR-006. Migration `AddLineStageSequence_RemoveProductionPlanStageSequenceNumber` đã generate VÀ apply thành công lên DB dev cục bộ (`dotnet ef database update` kết nối được MySQL thật). Solution build sạch (`dotnet build ProductionMES.sln`), 125/125 test pass (`dotnet test`, thêm mới `LineStageSequenceServiceTests`, sửa lại `ProductionPlanStageServiceTests`). Toàn bộ code chưa commit (giữ nguyên working tree theo yêu cầu) — UI chưa chạy thử bằng mắt (không có công cụ chụp màn hình Windows app trong phiên này), cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` để xác nhận trực quan. **17/08 (gap mới, cùng đợt)**: `LineStageSequencePage` cũng đang để `TextBox` gõ tay đổi `LineId` thay vì hiển thị Tên Line cố định readonly — đã bổ sung AC8, chưa implement. **17/08 (đã sửa AC8)**: bỏ hẳn `TextBox` gõ tay `LineId`, thay bằng `TextBlock` readonly hiển thị `LineName` (tra qua `ILineApiClient` mới, US-01) — Line vẫn cố định theo `StationOptions.LineId`, không cho đổi từ màn này. Cũng bổ sung seed permission `Line.View`/`Stage.View` cho role `Supervisor` trong `DbSeeder.EnsureSupervisorCatalogViewPermissionsAsync` (gap phát hiện thêm: `Stage.View` vốn đã cần cho combobox Công đoạn của chính màn này từ trước nhưng CHƯA từng được seed cho Supervisor — nếu không vá, `IStageApiClient.GetAllAsync()` sẽ luôn 403 với tài khoản Supervisor thật). Build `dotnet build src/ProductionMES.Station.Wpf` + `dotnet build src/ProductionMES.Infrastructure` pass, 125/125 test Application pass (không đổi Application layer). Api không build lại được trong phiên này do bị Visual Studio khoá file (đang chạy), chỉ build riêng từng project bị ảnh hưởng để xác nhận biên dịch đúng — UI vẫn CHƯA chạy thử bằng mắt trên app Windows thật | 2026-08-17 |
| US-04 | Quản lý trạm làm việc | ✅ Xong | Backend + UI web-admin (`a42c9f2`, `cedf66b`) | 2026-08-14 |
| US-04a | Quản lý API Key theo trạm | ✅ Xong | `4f0a3ed` | 2026-08-14 |
| US-05 | Tạo/cập nhật kế hoạch sản xuất | ✅ Xong | Backend (`0c5b944`, AC6 `9f0e299`) + UI `Station.Wpf` (`PlanSettingsPage`/`PlanSettingsViewModel`) đầy đủ AC1-AC6 và AC1a-AC1e: chọn Line theo tên (combobox, không gõ Id — AC1a-c, dùng `ILineApiClient` mới), nhập đủ ngày+giờ:phút cho Thời gian bắt đầu (AC1d), nhập/hiển thị Takt time dạng phút:giây tự quy đổi ra giây (AC1e, `Models/TaktTimeFormat.cs`), tự phục hồi "0:00"/"00:00" nếu xoá trắng rồi rời ô thay vì để trống (ngoại lệ hẹp trong AC1d/AC1e, `LostFocus` handler ở `PlanSettingsPage.xaml.cs`). Đồng bộ hiển thị ở DataGrid danh sách kế hoạch và màn "Chọn kế hoạch" (US-05b AC4). Build sạch, 125/125 test Application pass (không đổi Application layer). **17/08: người dùng đã tự chạy `Station.Wpf`, xác nhận trực quan toàn bộ hoạt động đúng** | 2026-08-17 |
| US-05a | Vòng đời trạng thái kế hoạch theo công đoạn | ✅ Xong | Backend xong (`0c5b944`). UI Áp dụng/Tạm dừng/Đóng code trong `PlanSelectionPage` (US-05b), gọi đúng `ProductionPlanStagesController`. **17/08: người dùng đã tự chạy app, xác nhận trực quan hoạt động đúng** | 2026-08-17 |
| US-05b | Chọn & áp dụng kế hoạch tại trạm | ✅ Xong | Backend `GET api/v1/production-plan-stages?lineId=&stageId=&includeClosed=` (`ProductionPlanStageSelectionController`) + UI `Station.Wpf` (`PlanSelectionPage`/`PlanSelectionViewModel`) đầy đủ AC1-AC5: combobox "Công đoạn" liệt kê đủ mọi công đoạn trong trình tự của Line (US-03, qua `ILineStageSequenceApiClient.GetByLineAsync` join tên `IStageApiClient`), không giới hạn theo công đoạn vật lý của trạm; tên Line hiển thị đúng (bỏ hardcode); Áp dụng/Tạm dừng/Đóng gọi đúng `ProductionPlanStagesController`. Build sạch, 125/125 test Application pass. **17/08: người dùng đã tự chạy app, xác nhận trực quan hoạt động đúng** | 2026-08-17 |
| US-06 | Tính sản lượng chuẩn theo giờ | ✅ Xong | Xác nhận có sẵn từ US-05, không cần code thêm (ghi chú trong `4f0a3ed`) | 2026-08-14 |
| US-07 | Scan tem tại trạm (luồng cơ bản) | ✅ Xong | Backend xong (`ceb0ee1`). **17/08**: UI `Station.Wpf` (AC2-AC5) đã code xong trên `AndonBoardWindow`/`AndonBoardViewModel`: bắt input HID scanner (TextBox ẩn `ScanInputBox`, Enter kết thúc 1 lượt scan) → gọi `IScanApiClient` (header `X-Station-Api-Key` qua `StationApiKeyHandler` mới, ADR-005) → banner OK (xanh, tự đóng 1.5s, AC3)/NG (đỏ, chờ bấm "Đã đọc, đóng", AC4)/Waiting (vàng, khi đang gửi) theo mockup đã chốt, banner góc trên-phải không che vùng tên trạm/công đoạn/số lượng ở Row 0 (AC5); chỉ số "Số lượng đã scan OK" (AC2) cập nhật qua sự kiện SignalR `ScanRecorded` (`IScanHubClient`/`ScanHubClient` mới, join group theo `WorkStationId`, tự re-join sau reconnect), không dựa vào response của chính request POST vừa gửi. Bổ sung `StationOptions.WorkStationId`/`StationApiKeyValue` (placeholder rỗng trong `appsettings.json`, giá trị thật phải cấu hình cục bộ thủ công). **17/08 (sau)**: đồng bộ format enum `Result` giữa HTTP và SignalR — `Program.cs` thêm `AddSignalR().AddJsonProtocol(...JsonStringEnumConverter)` (trước đó SignalR trả enum dạng số, khác HTTP trả chuỗi); `ScanHubClient` (Station.Wpf) thêm `.AddJsonProtocol(...)` tương ứng phía client (cần thêm package `Microsoft.AspNetCore.SignalR.Protocols.Json` 10.0.10 + `using Microsoft.Extensions.DependencyInjection;` — extension method nằm ở namespace đó, không phải `Microsoft.AspNetCore.SignalR.Client`). Build `dotnet build src/ProductionMES.Station.Wpf` + `dotnet build ProductionMES.sln` (trừ `ProductionMES.Api` bị Visual Studio/process đang chạy khoá file DLL — lỗi copy MSB3027/MSB3021 ngoài tầm kiểm soát, không phải lỗi biên dịch, không có `error CS` nào trong log) đều pass, 0 warning; 125/125 test Application pass (không đổi Application layer, không có test project riêng cho `Station.Wpf`). Toàn bộ code CHƯA commit (giữ nguyên working tree). **Chưa xác nhận trực quan bằng mắt** trên app Windows thật với máy scan HID thật hoặc API chạy thật — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` (kèm API + issue API Key trạm thật qua `StationApiKeysController` rồi điền vào `StationApiKeyValue`) để xác nhận AC2-AC5 hoạt động đúng trước khi đổi trạng thái sang ✅ Xong. **17/08 (bổ sung theo quyết định ghi vào SRS mục 5.1/8.2)**: chốt dùng keyboard-wedge (`TextBox` ẩn `ScanInputBox`) làm giải pháp chính thức thay đọc trực tiếp HID theo VendorID/ProductID; bổ sung cờ cấu hình cục bộ `StationOptions.EnableManualScanInput` (bool, mặc định `false`, đọc từ `appsettings.json` tại trạm) — khi `true`, `AndonBoardWindow` hiện thêm khu vực nhập tay mã tem (góc dưới-trái, gọi lại đúng `AndonBoardViewModel.HandleScanAsync` qua `SubmitManualScanCommand`, không xử lý kết quả riêng) + banner cảnh báo "⚠ ĐANG BẬT CHẾ ĐỘ NHẬP TAY — TEST ONLY" luôn hiển thị liên tục dưới hàng tên trạm/công đoạn; khi `false` (mặc định) UI giữ nguyên 100% như trước. Đã xử lý xung đột focus phát sinh: `ScanInputBox_LostFocus` (vốn luôn tự giành lại focus) được sửa để KHÔNG cướp focus nếu người dùng đang chủ động gõ vào ô nhập tay mới, để không chặn việc gõ tay khi bật cờ — không đổi hành vi khi cờ tắt (ô nhập tay `Collapsed`, không thể nhận focus). Build `dotnet build src/ProductionMES.Station.Wpf/ProductionMES.Station.Wpf.csproj` pass, 0 warning/error. **17/08 (xác nhận trực quan)**: người dùng đã tự chạy `dotnet run --project src/ProductionMES.Station.Wpf` với API thật + API Key trạm thật, thử scan bằng máy quét HID thật (chế độ keyboard-wedge) và chế độ nhập tay — AC2-AC5 hoạt động đúng. Bảng PLAN/ACTUAL/BALANCE/NG-%NG đầy đủ theo mốc giờ trong mockup Andon board (xem `Documents/Layout WPF/Capture.PNG`) **không thuộc scope US-07** — cố ý tách sang **US-09** (⬜ chưa làm), US-07 chỉ cần chỉ số "Số lượng đã scan OK" đơn giản (AC2) + popup kết quả scan (AC3-AC5), đã đủ và đúng | 2026-08-17 |
| US-08 | Kiểm tra hợp lệ khi scan | ✅ Xong | `ceb0ee1` — rule backend, không có UI riêng | 2026-08-14 |
| US-09 | Hiển thị số lượng & chỉ số +/- tại trạm | 🟡 Một phần | **17/08**: Backend + UI `Station.Wpf` đã code xong đầy đủ AC1-AC6. Backend mới: `IAndonBoardService`/`AndonBoardService` (`src/ProductionMES.Application/Services/AndonBoard/`) — xác định "kế hoạch active của trạm" bằng đúng cách US-05a đã chốt (`ProductionPlanStage.PlanStatus = Running` theo cặp Line+Stage của trạm, giống `ScanService`), tính PLAN/ACTUAL/BALANCE/NG/%NG qua `AndonBoardCalculator` (static, thuần túy, tách riêng để test AC5/AC6 không cần mock DB) — công thức trừ khung giờ nghỉ (AC5/AC6) dùng cách "trừ phần giao [StartTime, at] với từng BreakWindow", tự nhiên thỏa "PLAN đứng lại khi đang trong giờ nghỉ" mà không cần if/else riêng. Endpoint mới `GET api/v1/andon-board` (`AndonBoardController`), cùng scheme `StationApiKey` (ADR-005) và cách lấy `WorkStationId` từ claim như `ScansController` — khác `ScanService`, endpoint này KHÔNG lỗi 409 khi (Line,Stage) chưa có kế hoạch Running (chỉ hiển thị, trả `HasActivePlan=false`). UI: `AndonBoardViewModel` thêm `Rows` (ObservableCollection), `PlanCumulative`/`Balance`/`NgCount`/`NgPercent`/`HasActivePlan`, tải qua `IAndonBoardApiClient` mới lúc `Window_Loaded` + làm mới định kỳ (`StationOptions.AndonBoardRefreshIntervalSeconds`, mặc định 30s, cho PLAN "trôi" theo thời gian và phát hiện mốc giờ mới — AC6), còn ACTUAL/BALANCE tăng tức thời (AC4, ≤1s) bằng cách tái dùng đúng sự kiện SignalR `ScanRecorded` đã có từ US-07 (cộng dồn client-side vào dòng "hiện tại", không gọi lại API). `AndonBoardWindow.xaml` thay placeholder bằng bảng thật (cột TIME/PLAN/ACTUAL/BALANCE, dòng hiện tại highlight nền xanh đậm) + 1 ô NG/%NG gộp bên cạnh, giữ nguyên nền tối/hex cục bộ theo ADR-007 (không dùng Light theme). Balance dương xanh (#4CAF50)/âm đỏ (#E53935) đúng AC2/AC3. **Gap cố ý theo đúng phạm vi giao việc**: ô "tên nhân viên" (PER) trong mockup gốc (`Documents/Layout WPF/Capture.PNG`) KHÔNG implement — entity `Scan` không có `UserId`/định danh operator (đúng thiết kế ADR-005, Operator không đăng nhập cá nhân ở luồng scan thường), cần BA quyết định nguồn dữ liệu trước khi làm, không tự chế. Các field header khác trong mockup (Model/Customer/Lot/Takt time hiển thị dạng chip, Starting/End time, đồng hồ ngày giờ, mã "8-YX0S") cũng KHÔNG thêm vì không nằm trong AC1-AC6 chính thức — tránh mở rộng ngoài phạm vi. **Giả định kỹ thuật cần lưu ý**: mọi phép tính giờ ở `AndonBoardCalculator`/`AndonBoardService` dùng giờ tường (wall-clock) tại máy chủ API (`DateTime.Now`), CÙNG quy ước "không quy đổi múi giờ" đã có sẵn từ US-05 với `ProductionPlan.StartTime` — giả định máy chủ API và nhà máy cùng múi giờ (hệ thống hiện chưa xử lý đa múi giờ ở bất kỳ đâu, không phải gap mới của US-09). Build `dotnet build ProductionMES.sln` sạch (0 lỗi biên dịch — riêng `ProductionMES.Api` bị lock file do `ProductionMES.Api.exe` đang chạy sẵn từ trước, chỉ lỗi copy MSB3027 ngoài tầm kiểm soát, không có `error CS` nào); 141/141 test Application pass (`dotnet test`, thêm mới `AndonBoardCalculatorTests` 11 test cho AC5/AC6 và `AndonBoardServiceTests` 5 test cho orchestration). Toàn bộ code CHƯA commit (giữ nguyên working tree). **CHƯA xác nhận trực quan bằng mắt** trên app Windows thật (không có công cụ chạy/chụp màn hình GUI trong phiên này) — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` với API + kế hoạch đang Running thật để xác nhận bảng hiển thị đúng, highlight dòng hiện tại, màu Balance, và AC4 (scan xong ACTUAL tăng ngay) trước khi đổi sang ✅ Xong. **17/08 (bổ sung header theo mockup gốc 13/08/2026 đã chốt với người dùng)**: thêm các field header trước đó bị thiếu — DATE/TIME đồng hồ realtime (client-side, `DispatcherTimer` mỗi giây, không gọi API), MODEL/LOT/PROD.PLAN/TAKT TIME (format phút:giây, tái dùng `TaktTimeFormat.ToDisplay`)/STARTING TIME/ô PER (`OperatorNames`) — map thẳng từ `ProductionPlan` đã load sẵn trong `AndonBoardService.GetForWorkStationAsync`, KHÔNG thêm field `EndTime` (không tồn tại trên entity, không có nguồn dữ liệu). Backend: `AndonBoardDto` thêm `Model`/`Lot`/`PlannedQuantity`/`TaktTimeSeconds`/`PlanStartTime`/`OperatorNames`, set trong `AndonBoardService` (chỉ khi `HasActivePlan=true`). `AndonBoardServiceTests` cập nhật assert field mới ở cả 2 nhánh có/không có kế hoạch active. `Station.Wpf`: mirror DTO, `AndonBoardViewModel` thêm property tương ứng + `CurrentDateLabel`/`CurrentTimeLabel` (đồng hồ), `AndonBoardWindow.xaml` thêm 3 dòng mới vào Row 0 (row-model, row-plan, row-shift) và đồng hồ DATE/TIME phía trên khối ACTUAL/PLAN/BALANCE cũ — **bảng PLAN/ACTUAL/BALANCE theo mốc giờ ở Row 1 giữ nguyên, không đổi**. Build `Application` + `Station.Wpf` riêng lẻ pass 0 lỗi (`ProductionMES.Api` vẫn bị Visual Studio khoá file, không phải lỗi biên dịch — 0 `error CS`); 141/141 test Application pass. Vẫn **CHƯA xác nhận trực quan** header mới trên app thật — giữ 🟡 **17/08 (gap NG/%NG, chờ US-18)**: rà soát lại phát hiện `AndonBoardService.NgCount` hiện tính bằng `scans.Count(s => s.Result != ScanResult.Ok)` — SAI, vì đếm cả `ScanResult.DuplicateTag`/`ScanResult.PreviousStageNotPassed` (lỗi thao tác operator), không phải lỗi chất lượng. Đã bổ sung ghi chú chéo vào mục Phụ thuộc của US-09 và US-18: nguồn đúng cho NG/%NG là `ScanResult.Ng` (US-18, chưa tồn tại) — tạm thời NG/%NG PHẢI trả về `0`/`0%` cho tới khi US-18 implement, lúc đó dev PHẢI quay lại sửa `AndonBoardService.NgCount`/`NgPercent`. **18/08 (gap đã sửa cùng US-18)**: `AndonBoardService.NgCount` nay đếm đúng `Result == ScanResult.Ng`; `NgPercent = NgCount/(OkCount+NgCount)*100` (loại bỏ DuplicateTag/PreviousStageNotPassed khỏi cả tử số lẫn mẫu số). `AndonBoardServiceTests` cập nhật theo. Chi tiết xem mục US-18. | 2026-08-17 |
| US-10 | Lưu & tra cứu lịch sử scan | ✅ Xong | **14/08**: AC1/AC4 (snapshot 6 field Customer/Model/Lot/Revision/PlannedQuantity/TaktTimeSeconds vào `Scan`) đã code + migration `AddScanSnapshotFields` + test pass (`9f0e299`). **18/08**: AC2 (tra cứu theo tem, sắp theo thời gian) + AC3 (lọc trạm/Line/khoảng thời gian, kết hợp AND, phân trang theo API-Conventions mục 9) đã code xong — `IScanService.GetHistoryAsync`/`ScanService.GetHistoryAsync` (Application) dùng `IRepository<Scan>.FindAsync` với 1 Expression kết hợp AND toàn bộ filter tùy chọn, sắp `ScannedAtUtc` tăng dần rồi phân trang trong Service (không thêm Dapper — dữ liệu lọc theo tem/trạm/Line/thời gian đã đủ đơn giản để dùng EF Core qua Repository sẵn có, theo đúng "EF Core cho CRUD/query đơn giản"); DTO mới `ScanHistoryQuery`/`ScanHistoryItemDto`/`PagedResult<T>` (`src/ProductionMES.Application/DTOs/Scans/`, `DTOs/Common/PagedResult.cs` — envelope `{items,totalCount,page,pageSize}` đầu tiên trong dự án áp đúng mục 9 API-Conventions, trước đây "chưa implement ở backend"). Endpoint mới `GET api/v1/scans/history` (`ScanHistoryController`, tách riêng khỏi `ScansController` vì khác auth scheme — `[Authorize(Policy=Scan.View)]` mặc định Bearer/cookie theo ADR-004, KHÔNG dùng `StationApiKey` như `ScansController.Create`/ADR-005), query params `tagCode/workStationId/lineId/from/to/page/pageSize` kết hợp AND. Permission mới `Scan.View` (`PermissionResource.Scan=7`, `PermissionPolicies.ScanView`) seed cho `Admin`+`Supervisor`+`Manager` (Manager lần đầu có permission — đúng vai trò "Ban quản lý xem báo cáo" của `UserRole.Manager`), có `EnsureScanViewPermissionGrantsAsync` (`DbSeeder`) phòng gap seed như US-05 Supervisor trước đây. Test mới `ScanServiceHistoryTests` (8 test: tìm theo tem + sắp xếp thời gian, lọc riêng trạm/Line/khoảng thời gian, kết hợp AND, phân trang đúng TotalCount, Page/PageSize không hợp lệ tự chỉnh mặc định, và AC4 test qua chính API tra cứu — snapshot không đổi dù `ProductionPlan` bị sửa sau). Build `dotnet build ProductionMES.sln` sạch (0 lỗi/0 warning), 149/149 test Application pass (`dotnet test`). **Giả định kỹ thuật** (không có trong AC, cần lưu ý nếu BA thấy cần chốt lại): mặc định `pageSize=20`, tự chỉnh về mặc định nếu `page<1` hoặc `pageSize` ngoài [1,200]; thứ tự sắp xếp cho AC3 (không chỉ AC2) cũng dùng tăng dần theo `ScannedAtUtc` để đơn giản hóa (1 code path chung cho cả AC2/AC3); so khớp `TagCode` là so khớp tuyệt đối (không tính case-insensitive dù MySQL collation mặc định có thể case-insensitive ở tầng DB thật). UI tra cứu (web-admin) CHƯA làm — ngoài phạm vi giao việc lần này (task chỉ yêu cầu AC2/AC3 ở tầng API) | 2026-08-18 |
| US-11 | Bật/tắt Arduino theo trạm | ⬜ Chưa làm | | 2026-08-14 |
| US-12 | Luồng scan chờ Arduino | ⬜ Chưa làm | | 2026-08-14 |
| US-13 | Timeout xác định kết quả Arduino | ⬜ Chưa làm | | 2026-08-14 |
| US-14 | Kết nối & phục hồi cổng COM | ⬜ Chưa làm | | 2026-08-14 |
| US-15 | Khôi phục trạng thái phiên khi mở lại | ⬜ Chưa làm | | 2026-08-14 |
| US-16 | Hàng đợi cục bộ chống mất lượt scan | ⬜ Chưa làm | | 2026-08-14 |
| US-17 | Hiển thị trạng thái đồng bộ trên UI | ⬜ Chưa làm | | 2026-08-14 |
| US-18 | Scan xác nhận sản phẩm NG | 🟡 Một phần | Backend + UI `Station.Wpf` (AC1-AC7 bản gốc) đã code + verify trực tiếp trên máy thật (đã fix bug "nút NG không phản hồi" — xem "BUG phát hiện + fix 18/08/2026" ở mục US-18). **18/08/2026 (thay đổi yêu cầu — bổ sung đăng nhập Tổ trưởng bắt buộc, ĐÃ TRIỂN KHAI LẠI)**: bấm nút/quét mã "NG" nay chặn ngay từ đầu bằng popup đăng nhập Tổ trưởng (re-auth mỗi lần, đúng 1 lần/lượt Scan NG, permission mới `Scan.ConfirmNg` seed Supervisor/Admin/Manager) — đủ AC1/AC2/AC2a/AC2b/AC2c/AC3/AC5/AC7 mới. Domain: `Scan.ConfirmedByUserId`/`ConfirmedByUserName` (nullable, không backfill dữ liệu Ng cũ) + `PermissionAction.ConfirmNg`; migration `AddScanConfirmedByFields`. `DbSeeder` seed `Scan.ConfirmNg` cho 3 role qua `EnsureScanConfirmNgPermissionGrantsAsync`. `IScanService.CreateNgAsync`/`ScanService` nhận thêm `confirmedByUserId`/`confirmedByUserName` (validate not-empty, ném `BusinessRuleException` nếu thiếu). API: tách `POST api/v1/scans/ng` sang Controller mới `ScanNgController` (route riêng, scheme Bearer mặc định + `[Authorize(Policy=Scan.ConfirmNg)]`) — quyết định kỹ thuật xử lý "2 danh tính cùng lúc": KHÔNG cố gộp StationApiKey+Bearer trên cùng 1 action; giữ Bearer làm nguồn xác thực DUY NHẤT (UserId/Username lấy từ claim `ClaimTypes.NameIdentifier`/`ClaimTypes.Name` — dùng Username làm `ConfirmedByUserName` thay vì FullName, vì thêm claim FullName mới sẽ buộc Controller reference `ProductionMES.Infrastructure` (`JwtTokenGenerator`), vi phạm luật layer CLAUDE.md), còn `WorkStationId` chuyển sang lấy từ request body (Station.Wpf tự khai đúng `StationOptions.WorkStationId` của chính nó, chấp nhận được vì không expose UI cho việc gõ tay). `ScansController.CreateNg` đã xoá (chuyển hẳn sang Controller mới), `ScansController.Create`/`GetNgReasonSuggestions` giữ nguyên StationApiKey. `Station.Wpf`: `ISupervisorAuthService.LoginForNgConfirmationAsync` (gọi `station-login` như luồng cũ nhưng KHÔNG ghi vào `ISupervisorSessionService` dùng chung — tránh ảnh hưởng phiên US-05/05a/05b); `LoginDialogViewModel` thêm `RequiredPermission`/`NgConfirmationLoginResult` (khi set, kiểm tra permission ngay tại popup theo AC2b, không đóng dialog nếu thiếu quyền); `LoginDialog` expose `ViewModel` để caller cấu hình/đọc kết quả; `AndonBoardViewModel.ActivateNgModeCommand` (đổi thành async) gọi `AuthenticateForNgMode()` (hiển thị `LoginDialog`, owner = `Application.Current.MainWindow`) TRƯỚC khi set `IsNgModeActive=true` và TRƯỚC khi start `_ngModeTimeoutTimer` (AC7 — mốc 30s tính sau đăng nhập); token dùng riêng lưu ở field `_ngScanAccessToken` (KHÔNG session dùng chung), xoá lại trong `DeactivateNgMode()` (hoàn tất/hủy/timeout) để bắt buộc đăng nhập lại cho lượt Scan NG kế tiếp — đã đăng nhập rồi mà chỉ rescan mã "NG" để gia hạn timeout (chưa quét tem lỗi) thì KHÔNG đăng nhập lại lần 2, đúng "đúng 1 lần/lượt". `IScanApiClient.CreateNgAsync` nhận thêm `supervisorAccessToken`, `ScanApiClient` tự gắn `Authorization: Bearer` cho đúng request đó (không đổi HttpClient/handler đăng ký, `StationApiKeyHandler` vẫn gắn `X-Station-Api-Key` cho các action khác của client này, endpoint Bearer-only bỏ qua header đó — vô hại). Build `dotnet build ProductionMES.sln` sạch (0 Warning/0 Error, kể cả `Station.Wpf`); `dotnet test tests/ProductionMES.Application.Tests` 162/162 pass (4 test mới trong `ScanServiceNgTests` cho `ConfirmedByUserId`/`ConfirmedByUserName`). **UI CHƯA chạy thử bằng mắt trên máy thật** (không có công cụ chạy/chụp màn hình Windows app trong phiên này) — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` để xác nhận popup đăng nhập chặn đúng thời điểm, AC2b (sai tài khoản/thiếu quyền không đóng popup)/AC2c (Hủy quay lại Scan OK), và AC7 (mốc 30s tính từ sau đăng nhập) hoạt động đúng — giữ 🟡 cho tới khi xác nhận trực quan. | 2026-08-18 |
| US-19 | Quy trình Rework | ⬜ Chưa làm | | 2026-08-14 |
| US-20 | Báo cáo tỷ lệ lỗi & nguyên nhân | ⬜ Chưa làm | | 2026-08-14 |
| US-21 | Báo cáo tổng hợp theo Line | ⬜ Chưa làm | | 2026-08-14 |
| US-22 | Quản lý người dùng & phân quyền | ✅ Xong | Backend + UI web-admin (`a42c9f2`, `cedf66b`) | 2026-08-14 |
| US-23 | Xuất báo cáo Excel | ⬜ Chưa làm | | 2026-08-14 |

---

## 3.1 Nhóm chức năng: Quản lý danh mục Line & Công đoạn

### US-01: Quản lý danh mục Line
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

### US-01a: Cấu hình khung giờ nghỉ theo Line
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình 0 hoặc nhiều khung giờ nghỉ (giờ bắt đầu, giờ kết thúc, ghi chú) cho từng Line
**Để** hệ thống có cơ sở trừ đúng thời gian nghỉ khi tính sản lượng kế hoạch lũy kế hiển thị tại màn hình trạm (FR-09a)

**Acceptance Criteria**
- **AC1 — Thêm khung giờ nghỉ cho Line**
  - Given tôi là Admin đang cấu hình 1 Line đã tồn tại (US-01)
  - When tôi thêm 1 khung giờ nghỉ (giờ bắt đầu, giờ kết thúc, ghi chú — vd nghỉ trưa 12:00–13:00, nghỉ giữa giờ 15:00–15:15) và lưu
  - Then khung giờ nghỉ được lưu, áp dụng chung cho **mọi kế hoạch sản xuất chạy trên Line đó** (không cấu hình riêng theo từng kế hoạch)
- **AC2 — Line có thể có nhiều khung giờ nghỉ**
  - Given Line đã có 1 khung giờ nghỉ (vd nghỉ trưa)
  - When Admin thêm thêm khung giờ nghỉ khác (vd nghỉ giữa giờ)
  - Then cả 2 khung đều được lưu và áp dụng đồng thời khi tính sản lượng kế hoạch lũy kế
- **AC3 — Sửa/xóa khung giờ nghỉ**
  - Given khung giờ nghỉ đã tồn tại
  - When Admin sửa giờ bắt đầu/kết thúc/ghi chú, hoặc xóa khung giờ nghỉ đó
  - Then thay đổi có hiệu lực ngay cho các lần tính sản lượng kế hoạch lũy kế tiếp theo, không ảnh hưởng số liệu lịch sử đã tính trước đó
- **AC4 — Line không cấu hình khung giờ nghỉ nào vẫn hoạt động bình thường**
  - Given 1 Line không có khung giờ nghỉ nào được cấu hình (0 khung giờ nghỉ)
  - When hệ thống tính sản lượng kế hoạch lũy kế cho Line đó (FR-09a)
  - Then tính liên tục theo thời gian làm việc thực tế đã trôi qua, không có phần trừ giờ nghỉ nào
- **AC5 — Từ chối khung giờ nghỉ không hợp lệ**
  - Given Admin đang thêm/sửa khung giờ nghỉ
  - When giờ kết thúc không lớn hơn giờ bắt đầu, hoặc khung giờ nghỉ mới chồng lấn 1 khung giờ nghỉ khác đã có của cùng Line
  - Then hệ thống từ chối lưu, báo lỗi rõ ràng

**Nguồn FR:** FR-01, FR-09a, mục 6 quy tắc 11
**Phụ thuộc:** US-01 (Line phải tồn tại trước). Là điều kiện tiên quyết để US-09 AC5/AC6 (trừ giờ nghỉ khi tính PLAN lũy kế tại màn hình trạm) tính đúng — nếu chưa có story này, US-09 chỉ thỏa được trường hợp "0 khung giờ nghỉ".
**Cờ cảnh báo mục 8.2:** Không.
**Ghi chú:** Story tách ra vì FR-01/FR-09a (khung giờ nghỉ) được bổ sung vào SRS **sau** khi US-01 đã code xong (13/08/2026 so với commit US-01 trước đó) — `Line.cs` hiện chưa có field nào cho khung giờ nghỉ.

---

### US-02: Quản lý danh mục Công đoạn (master)
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** thêm/sửa/vô hiệu hóa công đoạn dùng chung cho toàn hệ thống
**Để** có danh mục công đoạn (Lắp ráp, Thông điện, Ngoại quan…) tái sử dụng được cho nhiều Line khác nhau

**Acceptance Criteria**
- **AC1 — Thêm công đoạn master**
  - Given tôi là Admin
  - When tôi nhập tên công đoạn mới và lưu
  - Then công đoạn được thêm vào danh mục dùng chung, chưa gắn với Line/kế hoạch nào
- **AC2 — Công đoạn không gắn cố định 1 Line**
  - Given công đoạn "Thông điện" đã có trong danh mục
  - When công đoạn này được áp dụng vào kế hoạch của Line 1 và Line 2
  - Then cả 2 Line đều dùng chung 1 định danh công đoạn "Thông điện" (phục vụ đúng rule chống trùng tem toàn hệ thống ở FR-08)
- **AC3 — Vô hiệu hóa công đoạn**
  - Given công đoạn đang hoạt động và không còn được dùng trong kế hoạch active nào
  - When Admin vô hiệu hóa
  - Then công đoạn không còn xuất hiện trong danh sách chọn khi cấu hình kế hoạch mới (FR-03)

**Nguồn FR:** FR-02
**Phụ thuộc:** Không có (song song với US-01, có thể làm ngay sau/cùng US-01)
**Cờ cảnh báo mục 8.2:** Có — danh sách công đoạn cụ thể từng Line và công đoạn nào dùng Arduino chưa xác định; ảnh hưởng tới US-04 (SuDungArduino) khi cần biết chính xác công đoạn nào cần bật cờ này.

---

### US-03: Cấu hình trình tự công đoạn cho Line
**Là** Tổ trưởng / Admin
**Tôi muốn** thêm, bớt, sắp xếp trình tự công đoạn cho 1 Line sản xuất (cấu hình 1 lần, dùng chung cho mọi kế hoạch chạy trên Line đó)
**Để** xác định đúng chuỗi công đoạn và công đoạn "liền trước" của Line, áp dụng thống nhất cho toàn bộ kế hoạch sản xuất hiện tại/tương lai trên Line, phục vụ kiểm tra hợp lệ khi scan (FR-08)

> **Sửa lại 17/08/2026** — bản gốc viết nhầm trình tự công đoạn là cấu hình riêng của từng kế hoạch (`ProductionPlan`); đúng ra đây là cấu hình của **Line**, thiết lập 1 lần, KHÔNG cấu hình lại theo từng kế hoạch. Mọi kế hoạch tạo trên 1 Line đều đi qua đúng và đủ toàn bộ trình tự đã cấu hình cho Line đó (không có khái niệm 1 kế hoạch chỉ áp dụng 1 tập con công đoạn — đã xác nhận với người dùng).

**Acceptance Criteria**
- **AC1 — Thêm công đoạn vào trình tự của Line**
  - Given Line đã tồn tại và đang hoạt động
  - When tôi chọn 1 công đoạn từ danh mục master và thêm vào trình tự của Line
  - Then công đoạn xuất hiện trong danh sách trình tự của Line, chưa có số thứ tự hoặc theo trình tự mặc định cuối danh sách; mọi kế hoạch (Draft/Running/Paused) đang chạy trên Line đó lập tức có thể áp dụng công đoạn mới này
- **AC2 — Gỡ công đoạn khỏi trình tự của Line — chặn hẳn nếu đang có kế hoạch chạy dở**
  - Given công đoạn đang thuộc trình tự của Line
  - When tôi gỡ công đoạn đó khỏi trình tự của Line, mà đang có ít nhất 1 kế hoạch của Line đó ở trạng thái `Running`/`Paused` tại đúng công đoạn này (US-05a)
  - Then hệ thống **từ chối gỡ**, báo rõ lý do (đang có kế hoạch chạy dở) — phải Tạm dừng/Đóng kế hoạch tại công đoạn đó trước mới gỡ được; nếu không có kế hoạch nào chạy dở tại công đoạn đó thì gỡ bình thường, số thứ tự các công đoạn còn lại được điều chỉnh hợp lý (liên tục 1..n)
- **AC3 — Sắp xếp lại trình tự (kéo-thả/nhập số thứ tự)**
  - Given Line có từ 2 công đoạn trở lên trong trình tự
  - When tôi kéo-thả hoặc nhập lại số thứ tự
  - Then hệ thống lưu đúng trình tự mới của Line và tự xác định lại công đoạn "liền trước" của mỗi công đoạn; áp dụng ngay cho mọi kế hoạch chạy trên Line đó, không phân biệt kế hoạch cũ/mới
- **AC4 — Từ chối khi trùng số thứ tự**
  - Given tôi đang sắp xếp trình tự công đoạn của Line
  - When tôi lưu với 2 công đoạn có cùng số thứ tự
  - Then hệ thống từ chối lưu và báo lỗi rõ ràng
- **AC5 — Từ chối khi tạo vòng lặp**
  - Given cấu hình trình tự công đoạn của Line
  - When cấu hình dẫn đến 1 công đoạn xuất hiện quá 1 lần trong trình tự (gây vòng lặp khi suy "liền trước")
  - Then hệ thống từ chối lưu, báo lỗi
- **AC6 — Không hồi tố lịch sử scan khi đổi trình tự**
  - Given Line đang có kế hoạch chạy dở, các lượt scan trước đó đã ghi nhận theo trình tự cũ
  - When Tổ trưởng/Admin đổi trình tự (thêm/bớt/sắp xếp lại)
  - Then các lượt scan **đã ghi nhận trước đó giữ nguyên bất biến** (nhất quán FR-10/mục 6 quy tắc 14); trình tự mới chỉ áp dụng cho việc xác định "công đoạn liền trước" (FR-08) từ thời điểm đổi trở đi
- **AC7 — Thêm công đoạn mới vào Line đang có kế hoạch chạy, hiệu lực ngay** (AC-07 SRS)
  - Given Line đang có ít nhất 1 kế hoạch active (`Running`/`Paused`) tại 1 hoặc nhiều công đoạn khác
  - When Tổ trưởng/Admin thêm 1 công đoạn mới vào trình tự của Line
  - Then trình tự cập nhật ngay, không cần deploy lại phần mềm — *(AC-07 gốc)*
- **AC8 — Hiển thị tên Line cố định, không cho chọn** *(bổ sung 17/08/2026 — cùng đợt sửa gap "hiển thị Id thay vì tên" ở US-05 AC1a)*
  - Given Tổ trưởng đang ở màn hình "Trình tự công đoạn (Line)" tại 1 trạm cụ thể
  - When màn hình được tải
  - Then hiển thị rõ **Tên** Line đang cấu hình (lấy theo Line cố định của trạm, FR-04), ở dạng chỉ đọc (không phải ô nhập/combobox) — không hiển thị Id thô, không cho gõ tay đổi sang Line khác từ màn hình này (khác US-05b, nơi Tổ trưởng được phép thao tác công đoạn khác từ xa — ở đây trình tự luôn gắn với đúng Line của trạm)

**Nguồn FR:** FR-03
**Phụ thuộc:** US-01 (Line phải tồn tại), US-02 (danh mục công đoạn master). KHÔNG còn phụ thuộc US-05 — trình tự là cấu hình của Line, không cần kế hoạch tồn tại trước.
**Cờ cảnh báo mục 8.2:** Không trực tiếp, nhưng phụ thuộc gián tiếp vào danh sách công đoạn thực tế từng Line (điểm mở #1).
**UI:** `Station.Wpf` (chế độ Tổ trưởng đăng nhập nâng quyền tại trạm) — KHÔNG phải `web-admin`, xem ADR-002 mục "Cập nhật phạm vi" (12/08/2026). Giữ ở Station.Wpf theo xác nhận người dùng 17/08/2026 (Tổ trưởng thao tác tại xưởng, không cần máy admin riêng) — nhưng tách thành màn hình cấu hình cấp Line riêng, KHÔNG còn nằm trong màn "Cài đặt kế hoạch" (US-05) như bản implement cũ.

---

### US-04: Quản lý trạm làm việc
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình trạm làm việc gắn với 1 Line và 1 công đoạn cụ thể, kèm thông tin cổng COM nếu có Arduino
**Để** mỗi trạm hoạt động đúng phạm vi nghiệp vụ của nó (Line/công đoạn) và có đủ thông tin kết nối phần cứng khi cần

**Acceptance Criteria**
- **AC1 — Tạo trạm làm việc**
  - Given Line và công đoạn đã tồn tại trong danh mục
  - When Admin tạo trạm mới, chọn đúng 1 Line và 1 công đoạn
  - Then trạm được tạo, gắn cố định với Line/công đoạn đã chọn
- **AC2 — Cấu hình cổng COM khi trạm có Arduino**
  - Given trạm được đánh dấu có sử dụng Arduino (`SuDungArduino = true`, xem US-11)
  - When Admin nhập cổng COM, baud rate, giao thức lệnh
  - Then thông tin được lưu và dùng để trạm WPF kết nối Serial khi khởi động
- **AC3 — Trạm không dùng Arduino không yêu cầu cấu hình COM**
  - Given trạm có `SuDungArduino = false`
  - When Admin cấu hình trạm
  - Then hệ thống không bắt buộc nhập thông tin cổng COM

**Nguồn FR:** FR-04
**Phụ thuộc:** US-01 (Line), US-02 (Công đoạn)
**Cờ cảnh báo mục 8.2:** Có — model máy scan (điểm mở #3) và danh sách công đoạn dùng Arduino (điểm mở #2) ảnh hưởng trực tiếp tới cấu hình trạm; cần xác nhận trước khi triển khai thực tế tại xưởng (không ảnh hưởng phần thiết kế chức năng chung).

---

### US-04a: Quản lý API Key theo trạm
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấp, xem trạng thái, thu hồi và cấp lại API Key riêng cho từng trạm làm việc
**Để** `Station.Wpf` xác thực được vào luồng scan thường (`StationApiKey` scheme, ADR-005) mà Operator không phải đăng nhập cá nhân

**Acceptance Criteria**
- **AC1 — Cấp API Key cho trạm**
  - Given trạm đã tồn tại (US-04)
  - When Admin chọn "Cấp API Key" cho trạm đó
  - Then hệ thống sinh 1 key ngẫu nhiên đủ dài, hiển thị giá trị thô **đúng 1 lần duy nhất** ngay sau khi cấp để Admin sao chép vào file cấu hình cục bộ của trạm (`appsettings.json`); server chỉ lưu **hash** của key, không lưu giá trị thô (cùng nguyên tắc `RefreshToken.TokenHash` — ADR-003/ADR-005)
- **AC2 — Không xem lại được giá trị thô sau khi rời màn hình**
  - Given API Key đã được cấp và Admin đã đóng màn hình xác nhận
  - When Admin quay lại xem thông tin trạm sau đó
  - Then chỉ thấy metadata (ngày cấp, trạng thái Active/Revoked), không hiển thị lại được giá trị thô
- **AC3 — Thu hồi API Key**
  - Given trạm đang có 1 API Key ở trạng thái Active
  - When Admin chọn "Thu hồi"
  - Then key chuyển trạng thái Revoked (ghi nhận thời điểm thu hồi), mọi request `StationApiKey` dùng key đó bị từ chối ngay từ lần gọi kế tiếp
- **AC4 — Xoay vòng: cấp lại key mới cho trạm đã có key**
  - Given trạm nghi ngờ lộ key hoặc thay thiết bị, đang có 1 key Active
  - When Admin cấp lại key mới cho trạm đó
  - Then key cũ tự động chuyển Revoked, key mới có hiệu lực; lịch sử key cũ vẫn được giữ lại (không xóa bản ghi) để truy vết theo đúng thiết kế `StationApiKey` tách riêng khỏi `WorkStation` (ADR-005)
- **AC5 — Từ chối request không có/sai API Key**
  - Given request gọi endpoint dùng scheme `StationApiKey` (vd `POST api/v1/scans`) thiếu header `X-Station-Api-Key` hoặc giá trị không khớp hash đã lưu
  - When server xác thực
  - Then trả về 401, không xử lý request
- **AC6 — Từ chối khi key hợp lệ nhưng sai trạm**
  - Given API Key hợp lệ (hash khớp) nhưng thuộc về `WorkStationId` khác với `WorkStationId` gửi trong request body
  - When server xác thực
  - Then từ chối request — chống trạm A dùng key của mình gọi giả danh trạm B (ADR-005)

**Nguồn FR:** Không có FR tương ứng trực tiếp trong SRS — đây là yêu cầu kỹ thuật phát sinh từ `Documents/ADR-005-auth-station-wpf.md` (mục "Hệ quả/Tiêu cực", dòng 76: *"web-admin chưa có UI cho việc này, cần bổ sung vào US-04 hoặc 1 story riêng trước/song song khi code US-07/08"*), cần thiết để `Station.Wpf` xác thực được vào luồng scan.
**Phụ thuộc:** US-04 (trạm phải tồn tại trước khi cấp key). **Là điều kiện tiên quyết bắt buộc của US-07/US-08** — nếu chưa có story này, `Station.Wpf` không có cách nào lấy được API Key hợp lệ để gọi `POST api/v1/scans`.
**Cờ cảnh báo mục 8.2:** Không.
**UI:** `web-admin` (Admin) — gắn liền màn hình quản lý trạm (US-04), không phải `Station.Wpf`.

---

## 3.2 Nhóm chức năng: Kế hoạch sản xuất

### US-05: Tạo/cập nhật kế hoạch sản xuất (màn hình "Cài đặt kế hoạch")
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

**Nguồn FR:** FR-05, mục 6 quy tắc 14 (AC6)
**Phụ thuộc:** US-01 (Line). AC6 (khóa tuyệt đối khi đã có scan) cần entity `Scan` đã tồn tại (US-07/US-08, đã xong — `ceb0ee1`) để kiểm tra "đã có ≥1 bản ghi scan"; nên triển khai AC6 cùng đợt với US-10 AC4 vì cả 2 cùng chạm `ProductionPlanService`/entity `Scan`.
**Cờ cảnh báo mục 8.2:** Không trực tiếp.
**UI:** `Station.Wpf` (chế độ Tổ trưởng đăng nhập nâng quyền tại trạm) — KHÔNG phải `web-admin`, xem ADR-002 mục "Cập nhật phạm vi" (12/08/2026).
**Ghi chú:** Cập nhật 13/08/2026 theo FR-05 mới (bổ sung Khách hàng/Model/Lot/Revision, bỏ "ca làm việc", đổi nghĩa "tên nhân viên" thành người vận hành chứ không phải người đăng nhập thao tác màn hình). Phần ràng buộc "1 kế hoạch active" và vòng đời trạng thái đã tách hẳn sang **US-05a** (trước đây là AC2 của story này) vì trạng thái nay gắn cấp (Kế hoạch, Công đoạn), không phải cấp Kế hoạch. **Cập nhật 14/08/2026**: bổ sung AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi đã có scan) — gap phát hiện khi rà soát US-10 (lịch sử scan tra cứu qua join động, dễ hiển thị sai nếu kế hoạch bị sửa sau khi đã scan). AC6 **nên triển khai cùng đợt** với US-10 AC4 (snapshot 6 field vào `Scan`) vì cả 2 cùng xử lý 1 gap, cùng chạm entity `Scan`/`ProductionPlanService` — xem SRS mục 6 quy tắc 14, mục 8.1.

---

### US-05a: Vòng đời trạng thái kế hoạch theo từng công đoạn (tạm dừng — chạy lại — đóng độc lập)
**Là** Tổ trưởng
**Tôi muốn** mỗi công đoạn áp dụng 1 kế hoạch có vòng đời trạng thái riêng (`Draft/Running/Paused/Completed/Cancelled`), tiến độ "đã chạy/còn lại" luôn tính đúng theo thời gian thực, và có thể tạm dừng hoặc đóng độc lập từng công đoạn
**Để** tạm dừng 1 lô đang chạy dở (đổi Line/trạm sang việc khác) rồi chạy lại chính xác sau này mà không mất/nhầm số lượng còn lại, đồng thời cho phép công đoạn nào xong trước tự đóng mà không phải chờ các công đoạn khác của cùng kế hoạch

**Acceptance Criteria**
- **AC1 — Áp dụng kế hoạch cho 1 (Line, Công đoạn), chuyển `Running`**
  - Given kế hoạch đang `Draft` hoặc `Paused` cho công đoạn X của Line Y
  - When Tổ trưởng bấm "Áp dụng"
  - Then kế hoạch chuyển `Running` cho đúng cặp (Line Y, Công đoạn X); nếu (Line Y, Công đoạn X) đang có 1 kế hoạch khác ở `Running`, hệ thống từ chối và yêu cầu Tạm dừng/Đóng kế hoạch đó trước
- **AC2 — Ràng buộc active theo (Line, Công đoạn), không theo cả Line**
  - Given công đoạn A của Line 1 đang `Running` kế hoạch X
  - When Tổ trưởng áp dụng kế hoạch Y cho công đoạn B của cùng Line 1
  - Then được chấp nhận bình thường — công đoạn B chạy kế hoạch khác song song, không bị chặn bởi trạng thái của công đoạn A (đúng thực tế dây chuyền có WIP giữa các trạm)
- **AC3 — Tạm dừng, giữ nguyên tiến độ**
  - Given kế hoạch đang `Running` tại 1 (Line, Công đoạn)
  - When Tổ trưởng bấm "Tạm dừng"
  - Then chuyển `Paused`; tiến độ không mất (vì tính động từ lịch sử scan OK, xem AC4), có thể "Áp dụng" lại bất kỳ lúc nào sau này
- **AC4 — Tính "đã chạy"/"còn lại" động, không lưu số liệu tĩnh**
  - Given kế hoạch đã có lượt scan OK tại đúng công đoạn đó
  - When xem tiến độ (màn hình chọn kế hoạch — US-05b, hoặc dashboard trạm)
  - Then "đã chạy" = tổng số lượt scan kết quả OK theo đúng cặp (kế hoạch, công đoạn); "còn lại" = số lượng kế hoạch − đã chạy; giá trị luôn tính lại theo dữ liệu scan hiện có, không đọc từ 1 cột số lượng lưu sẵn
- **AC5 — Tự động `Completed` khi đủ số lượng**
  - Given 1 cặp (kế hoạch, công đoạn) đang `Running`, số đã chạy sắp đạt đủ số lượng kế hoạch
  - When có thêm 1 lượt scan OK làm đủ số lượng
  - Then hệ thống tự động chuyển cặp đó sang `Completed` ngay, không cần Tổ trưởng thao tác thêm
- **AC6 — Đóng sớm thủ công, yêu cầu xác nhận nếu chưa đủ số lượng**
  - Given 1 cặp (kế hoạch, công đoạn) đang `Running`/`Paused`, số đã chạy còn thấp hơn số lượng kế hoạch
  - When Tổ trưởng bấm "Đóng kế hoạch"
  - Then hệ thống hiển thị rõ số lượng còn thiếu, yêu cầu xác nhận trước khi chuyển sang `Cancelled`
- **AC7 — Kế hoạch `Completed`/`Cancelled` không tự "Áp dụng" lại được như `Paused`**
  - Given 1 cặp (kế hoạch, công đoạn) đã `Completed` hoặc `Cancelled`
  - When Tổ trưởng mở màn hình chọn kế hoạch
  - Then cặp đó không xuất hiện trong danh sách áp dụng mặc định (US-05b AC2) — coi như đã kết thúc vòng đời tại công đoạn đó

**Nguồn FR:** FR-05a, mục 6 quy tắc 12
**Phụ thuộc:** US-05 (kế hoạch phải được tạo trước), US-03 (cần biết công đoạn nào thuộc trình tự của Line, để suy ra danh sách (Kế hoạch, Công đoạn) cần theo dõi vòng đời), US-08 (nguồn dữ liệu scan OK để tính tiến độ động)
**Cờ cảnh báo mục 8.2:** Không (đã chốt đầy đủ với người dùng ngày 13/08/2026).
**Lưu ý kỹ thuật:** Thay đổi so với thiết kế backend ban đầu (`ProductionPlanService.ActivateAsync`/`DeactivateAsync` hiện chỉ kiểm tra duy nhất theo `LineId`, cờ `IsActive` dạng bool) — cần đổi model sang trạng thái theo cặp `(LineId, StageId)` khi implement, không chỉ đổi UI.

---

### US-05b: Chọn & áp dụng kế hoạch cho công đoạn tại trạm (màn hình "Chọn kế hoạch")
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

### US-06: Tính và hiển thị sản lượng chuẩn theo giờ
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

## 3.3 Nhóm chức năng: Scan tem & theo dõi sản xuất

### US-07: Scan tem tại trạm (luồng cơ bản)
**Là** Công nhân vận hành trạm
**Tôi muốn** scan tem sản phẩm và nhận thông báo kết quả rõ ràng
**Để** ghi nhận sản lượng đúng và biết ngay lượt scan có hợp lệ hay không mà không làm chậm nhịp thao tác

**Acceptance Criteria**
- **AC1 — Ghi nhận thông tin lượt scan**
  - Given trạm đang có kế hoạch active
  - When công nhân scan tem sản phẩm
  - Then hệ thống ghi nhận thời gian, trạm, công đoạn, Line, kế hoạch tương ứng
- **AC2 — Cập nhật số lượng real-time sau scan hợp lệ**
  - Given lượt scan hợp lệ (OK)
  - When hệ thống xử lý xong
  - Then số lượng đã scan tại trạm cập nhật ngay trên màn hình, không cần làm mới thủ công
- **AC3 — Thông báo xác nhận khi scan OK** *(AC-11 gốc)*
  - Given scan tem hợp lệ (kết quả OK)
  - When hệ thống xử lý xong
  - Then hiển thị thông báo xác nhận đã lưu kèm mã tem, tự động biến mất sau 1–2 giây, không che số liệu chính, không chặn thao tác scan tiếp theo *(AC-11)*
- **AC4 — Thông báo lỗi khi scan bị từ chối** *(AC-12 gốc)*
  - Given scan tem bị từ chối (trùng tem hoặc chưa qua công đoạn trước)
  - When hệ thống trả kết quả lỗi
  - Then hiển thị thông báo lỗi rõ ràng, khác biệt màu sắc/âm thanh so với thông báo OK, yêu cầu người vận hành xác nhận đã đọc trước khi tắt *(AC-12)*
- **AC5 — Vị trí hiển thị thông báo không che số liệu chính**
  - Given đang ở màn hình trạm
  - When có thông báo OK hoặc lỗi xuất hiện
  - Then thông báo hiển thị ở góc màn hình/dạng banner nhỏ, không đè lên vùng hiển thị số lượng/chỉ số +/-

**Nguồn FR:** FR-07
**Phụ thuộc:** US-04 (trạm làm việc), US-04a (API Key theo trạm — bắt buộc để `Station.Wpf` xác thực được, xem ADR-005), US-05/US-05a/US-05b (kế hoạch đã tạo và ở trạng thái `Running` cho đúng (Line, Công đoạn) của trạm), US-08 (business rule kiểm tra hợp lệ — thực thi song song vì FR-07 mô tả UI/UX, FR-08 mô tả rule; nên triển khai cùng đợt)
**Cờ cảnh báo mục 8.2:** Có — phụ thuộc model máy scan Zebra DS2208 (điểm mở #3) cho phần giao tiếp HID thực tế tại trạm.

---

### US-08: Kiểm tra hợp lệ khi scan (chống trùng tem & kiểm tra công đoạn liền trước)
**Là** Hệ thống (business rule nền tảng), phục vụ Công nhân/Tổ trưởng
**Tôi muốn** kiểm tra trùng tem theo công đoạn và kiểm tra đã qua công đoạn liền trước, trên phạm vi toàn hệ thống (không phân biệt Line)
**Để** đảm bảo tính toàn vẹn dữ liệu sản xuất, tránh ghi nhận sai lệch sản lượng và trình tự công đoạn

**Acceptance Criteria**
- **AC1 — Trùng tem cùng công đoạn khác Line** *(AC-01 gốc)*
  - Given scan tem A tại công đoạn "Lắp ráp" Line 1
  - When sau đó scan tem A tại công đoạn "Lắp ráp" Line 2
  - Then lần 2 bị từ chối, báo "Trùng tem tại công đoạn này" *(AC-01)*
- **AC2 — Khác công đoạn, khác Line hợp lệ** *(AC-02 gốc)*
  - Given scan tem A tại "Lắp ráp" Line 1
  - When sau đó scan tem A tại "Thông điện" Line 2
  - Then lần 2 được chấp nhận *(AC-02)*
- **AC3 — Chưa qua công đoạn liền trước** *(AC-03 gốc)*
  - Given tem B chưa từng scan "Lắp ráp" ở bất kỳ Line nào
  - When scan tem B tại "Thông điện"
  - Then bị từ chối, báo rõ "Chưa qua công đoạn: Lắp ráp" *(AC-03)*
- **AC4 — Scan hợp lệ khi qua đủ 2 bước kiểm tra**
  - Given tem chưa trùng công đoạn hiện tại và đã qua công đoạn liền trước
  - When công nhân scan
  - Then lượt scan được ghi nhận OK
- **AC5 — Không xử lý race condition giữa 2 Line cho cùng 1 tem** (theo mục 8.1 SRS)
  - Given tem chỉ tồn tại vật lý ở đúng 1 vị trí tại 1 thời điểm
  - When xử lý transaction ghi nhận lượt scan
  - Then xử lý theo transaction thông thường, không cần cơ chế khóa/đồng bộ đặc biệt cho race condition 2 Line

**Nguồn FR:** FR-08, mục 6 quy tắc 3-4, mục 8.1
**Phụ thuộc:** US-03 (trình tự công đoạn — để xác định công đoạn "liền trước"), US-07 (luồng scan cơ bản)
**Cờ cảnh báo mục 8.2:** Không trực tiếp (rule đã chốt rõ ràng).

---

### US-09: Hiển thị số lượng và chỉ số âm/dương tại trạm
**Là** Công nhân vận hành trạm / Tổ trưởng
**Tôi muốn** xem số lượng đã scan OK, sản lượng kế hoạch lũy kế, và chênh lệch thực tế-kế hoạch theo thời gian thực
**Để** biết ngay tiến độ sản xuất đang vượt hay trễ so với kế hoạch

**Acceptance Criteria**
- **AC1 — Hiển thị đủ 3 giá trị**
  - Given trạm đang có kế hoạch active
  - When công nhân xem màn hình trạm
  - Then hiển thị: số lượng đã scan OK lũy kế, sản lượng kế hoạch lũy kế đến hiện tại, và chênh lệch (Thực tế − Kế hoạch)
- **AC2 — Chênh lệch dương hiển thị màu xanh** *(AC-05 gốc)*
  - Given sản lượng thực tế cao hơn kế hoạch lũy kế
  - When hệ thống tính chỉ số
  - Then chỉ số hiển thị dương, màu xanh *(AC-05)*
- **AC3 — Chênh lệch âm hiển thị màu đỏ**
  - Given sản lượng thực tế thấp hơn kế hoạch lũy kế
  - When hệ thống tính chỉ số
  - Then chỉ số hiển thị âm, màu đỏ
- **AC4 — Cập nhật real-time không cần làm mới thủ công**
  - Given có lượt scan mới hợp lệ
  - When hệ thống ghi nhận xong
  - Then giá trị trên màn hình trạm cập nhật ngay (qua SignalR, độ trễ ≤ 1 giây theo NFR), không cần thao tác làm mới
- **AC5 — Trừ thời gian nghỉ khi tính sản lượng kế hoạch lũy kế** *(bổ sung SRS 13/08/2026, FR-09a)*
  - Given Line của trạm đang trong 1 khung giờ nghỉ đã cấu hình (US-01a)
  - When hệ thống tính sản lượng kế hoạch lũy kế để hiển thị tại màn hình trạm
  - Then giá trị PLAN lũy kế **dừng tăng**, giữ nguyên bằng giá trị tại thời điểm bắt đầu khung giờ nghỉ; sau khi hết nghỉ, tính tiếp bình thường theo thời gian làm việc thực tế đã trôi qua — không thay đổi công thức sản lượng chuẩn/giờ gốc (FR-06)
- **AC6 — Bảng theo dõi sản lượng theo mốc giờ hiển thị đúng trong lúc nghỉ**
  - Given màn hình trạm có bảng theo dõi sản lượng theo từng mốc giờ trong ca
  - When 1 mốc giờ hiển thị trên bảng rơi vào khung giờ nghỉ đã cấu hình cho Line
  - Then mốc giờ đó **vẫn hiển thị** trên bảng, nhưng cột PLAN lũy kế giữ nguyên giá trị tại thời điểm bắt đầu nghỉ (không cộng thêm cho tới khi hết khung giờ nghỉ)

**Nguồn FR:** FR-09, FR-09a, mục 6 quy tắc 5, quy tắc 11
**Phụ thuộc:** US-07, US-08 (cần luồng scan và kết quả OK để tính số liệu), US-01a (khung giờ nghỉ theo Line — bắt buộc để AC5/AC6 ở trên tính đúng; nếu chưa có US-01a, US-09 chỉ triển khai được đúng trường hợp "0 khung giờ nghỉ"), US-18 (Scan xác nhận sản phẩm NG — nguồn dữ liệu duy nhất hợp lệ cho ô NG/%NG trên Andon board là `Result = ScanResult.Ng`; **không** được đếm `ScanResult.DuplicateTag`/`ScanResult.PreviousStageNotPassed` vào NG vì đó là lỗi thao tác của operator — quét trùng tem, quét sai thứ tự công đoạn — không phải lỗi chất lượng sản phẩm. Trước khi US-18 hoàn thành và giá trị `ScanResult.Ng` tồn tại, ô NG/%NG trên Andon board PHẢI tạm hiển thị `0`/`0%`, không được lấy số liệu từ 2 giá trị reject kể trên)
**Cờ cảnh báo mục 8.2:** Không.

---

### US-10: Lưu và tra cứu lịch sử scan
**Là** Tổ trưởng / Admin / Ban quản lý
**Tôi muốn** mọi lượt scan (kể cả lỗi) được lưu lại và có thể tra cứu theo nhiều tiêu chí
**Để** phục vụ truy vết, đối chiếu, và làm nền tảng dữ liệu cho báo cáo sau này

**Acceptance Criteria**
- **AC1 — Lưu đầy đủ mọi lượt scan**
  - Given công nhân thực hiện 1 lượt scan (dù kết quả OK, Trùng tem, hay Chưa qua công đoạn trước)
  - When hệ thống xử lý
  - Then lượt scan được lưu lại với đầy đủ: mã tem, thời gian, trạm, công đoạn, Line, kế hoạch, kết quả, người thao tác — kèm **snapshot** Khách hàng/Model/Lot/Revision/Số lượng kế hoạch/Takt time của kế hoạch đó tại đúng thời điểm scan (không phải giá trị hiện tại của kế hoạch, tránh sai lệch nếu kế hoạch bị sửa Số lượng/Takt time sau này — xem US-05 AC6)
- **AC2 — Tra cứu theo tem**
  - Given có dữ liệu lịch sử scan
  - When người dùng tìm theo mã tem
  - Then trả về toàn bộ lượt scan liên quan tới tem đó, sắp xếp theo thời gian
- **AC3 — Tra cứu theo trạm/Line/khoảng thời gian**
  - Given có dữ liệu lịch sử scan
  - When người dùng lọc theo trạm, hoặc theo Line, hoặc theo khoảng thời gian (có thể kết hợp)
  - Then trả về đúng tập kết quả phù hợp bộ lọc
- **AC4 — Lịch sử scan không đổi theo giá trị hiện tại của kế hoạch (snapshot bất biến)**
  - Given tem đã được scan tại 1 công đoạn khi kế hoạch có Số lượng = 1000, Takt time = 30s
  - When sau đó Tổ trưởng sửa Số lượng kế hoạch thành 1200 (có Confirm — US-05 AC5; Khách hàng/Model/Lot/Revision không sửa được vì đã có scan, xem US-05 AC6)
  - Then khi tra cứu lại lượt scan cũ (AC2/AC3), Số lượng/Takt time hiển thị vẫn là **1000/30s** (giá trị tại thời điểm scan), không tự đổi theo 1200 hiện tại của kế hoạch

**Nguồn FR:** FR-10, mục 6 quy tắc 6, quy tắc 14
**Phụ thuộc:** US-07, US-08. AC4 (snapshot bất biến) nên triển khai **cùng đợt** với US-05 AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi đã có scan) — cả 2 cùng xử lý 1 gap phát hiện ngày 14/08/2026, cùng chạm entity `Scan`/`ProductionPlanService`.
**Cờ cảnh báo mục 8.2:** Không.
**Ghi chú:** Cập nhật 14/08/2026 — AC1 bổ sung yêu cầu lưu snapshot 6 field (Customer/Model/Lot/Revision/PlannedQuantity/TaktTimeSeconds) vào `Scan` tại thời điểm scan; AC4 (mới) khẳng định tính bất biến. **Hiện trạng (`ceb0ee1`)**: entity `Scan` (`src/ProductionMES.Domain/Entities/Scan.cs`) mới chỉ có `ProductionPlanId` tham chiếu, CHƯA có 6 field snapshot — phần "tra cứu theo tem/trạm/Line/thời gian" (AC2/AC3) và phần snapshot (AC1/AC4) đều chưa làm.

---

## 3.4 Nhóm chức năng: Giao tiếp Arduino (kiểm tra tự động)

### US-11: Cấu hình bật/tắt sử dụng Arduino theo từng trạm
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** cấu hình cờ `SuDungArduino` (bật/tắt) độc lập cho từng trạm
**Để** trạm nào không có Arduino vẫn hoạt động bình thường theo luồng scan thủ công, không bị ảnh hưởng bởi logic Arduino

**Acceptance Criteria**
- **AC1 — Trạm không dùng Arduino hoạt động luồng thủ công bình thường** *(AC-20 gốc)*
  - Given trạm có `SuDungArduino = false`
  - When công nhân scan tem tại trạm này
  - Then hoạt động hoàn toàn theo luồng scan thủ công bình thường (FR-07/FR-08/FR-18), không có bước chờ Arduino *(AC-20)*
- **AC2 — Bật Arduino kích hoạt state machine**
  - Given trạm có `SuDungArduino = true`
  - When công nhân scan tem
  - Then trạm hoạt động theo state machine mô tả tại US-12 (FR-12), thay vì luồng thủ công đơn thuần

**Nguồn FR:** FR-11
**Phụ thuộc:** US-04 (quản lý trạm làm việc)
**Cờ cảnh báo mục 8.2:** Có — cần xác nhận danh sách công đoạn nào thực sự dùng Arduino (điểm mở #2) trước khi cấu hình `SuDungArduino = true` cho trạm cụ thể tại xưởng.

---

### US-12: Luồng "Scan tem trước → chờ kết quả kiểm tra từ Arduino" khi có Arduino
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

### US-13: Timeout xác định kết quả kiểm tra Arduino
**Là** Hệ thống / Admin cấu hình trạm
**Tôi muốn** đếm timeout kể từ lúc scan tem và suy luận NG khi hết thời gian mà không nhận "OK"
**Để** hệ thống không bị treo vô thời hạn chờ tín hiệu Arduino trong trường hợp thiết bị không đạt

**Acceptance Criteria**
- **AC1 — Timeout mặc định 45 giây**
  - Given trạm dùng Arduino chưa có cấu hình riêng
  - When tính timeout chờ kết quả
  - Then hệ thống áp dụng mặc định 45 giây
- **AC2 — Cấu hình timeout riêng theo từng trạm qua file cấu hình cục bộ**
  - Given file cấu hình cục bộ (`appsettings.json`) tại 1 trạm có giá trị timeout khác mặc định
  - When trạm khởi động và chờ kết quả Arduino
  - Then áp dụng đúng giá trị đã cấu hình riêng cho trạm đó, không cần sửa code/deploy lại
- **AC3 — Hết timeout chuyển sang bước xác nhận NG**
  - Given đã scan tem và bắt đầu đếm timeout
  - When hết thời gian timeout mà không nhận "OK"
  - Then hệ thống chuyển sang bước xác nhận NG (AC4 của US-12 / AC-19)

**Nguồn FR:** FR-13
**Phụ thuộc:** US-12 (là 1 phần cấu thành state machine của US-12, có thể coi là sub-story kỹ thuật gắn liền)
**Cờ cảnh báo mục 8.2:** Không trực tiếp, nhưng giá trị timeout đã chốt cấu hình cục bộ per-trạm (không phải điểm mở).

---

### US-14: Kết nối & phục hồi cổng COM với Arduino
**Là** Công nhân vận hành trạm / Hệ thống
**Tôi muốn** ứng dụng trạm tự động mở và phục hồi kết nối cổng COM, đồng thời ghi log dữ liệu Serial
**Để** đảm bảo kết quả OK/NG từ Arduino luôn đáng tin cậy và có thể truy vết khi có sự cố phần cứng

**Acceptance Criteria**
- **AC1 — Tự động mở kết nối khi khởi động**
  - Given trạm có `SuDungArduino = true`
  - When ứng dụng WPF khởi động
  - Then tự động mở kết nối cổng COM theo cấu hình đã lưu (FR-04)
- **AC2 — Tự kết nối lại khi mất kết nối**
  - Given kết nối COM bị mất trong lúc vận hành
  - When hệ thống phát hiện mất kết nối
  - Then tự động thử kết nối lại, không cần khởi động lại ứng dụng thủ công
- **AC3 — Không cho scan trong lúc mất kết nối**
  - Given cổng COM đang mất kết nối tại trạm dùng Arduino
  - When công nhân cố gắng scan tem
  - Then hệ thống hiển thị rõ trạng thái lỗi thiết bị và không cho scan (vì không thể tin cậy kết quả OK/NG lúc này)
- **AC4 — Log toàn bộ dữ liệu gửi/nhận Serial**
  - Given trạm đang giao tiếp Serial với Arduino
  - When có dữ liệu gửi hoặc nhận qua cổng COM
  - Then hệ thống ghi log lại (bảng `LichSuLenhArduino`) phục vụ truy vết sự cố phần cứng

**Nguồn FR:** FR-14
**Phụ thuộc:** US-04 (cấu hình COM tại trạm), US-11
**Cờ cảnh báo mục 8.2:** Không trực tiếp.

---

## 3.5 Nhóm chức năng: Khôi phục trạng thái & chống mất dữ liệu

### US-15: Khôi phục trạng thái phiên làm việc khi khởi động lại (tắt/mở bình thường)
**Là** Công nhân vận hành trạm
**Tôi muốn** ứng dụng trạm tự động hiển thị lại đúng kế hoạch, số lượng, chỉ số +/- khi mở lại
**Để** không phải chọn lại cấu hình từ đầu sau khi tắt/mở ứng dụng bình thường

**Acceptance Criteria**
- **AC1 — Khôi phục đúng trạng thái sau khi mở lại** *(AC-09 gốc)*
  - Given tắt ứng dụng bình thường rồi mở lại
  - When ứng dụng khởi động, gọi API lấy lại trạng thái từ server
  - Then hiển thị lại đúng kế hoạch, số lượng, chỉ số +/- như trước khi tắt, không cần chọn lại cấu hình *(AC-09)*
- **AC2 — Dữ liệu nguồn từ server, UI chỉ đồng bộ lại**
  - Given dữ liệu kế hoạch/số lượng/chỉ số đã lưu tại server (MySQL)
  - When ứng dụng khởi động lại
  - Then chỉ cần đồng bộ giao diện đúng theo trạng thái server, không tính toán lại từ đầu phía client

**Nguồn FR:** FR-15
**Phụ thuộc:** US-05 (kế hoạch active), US-09 (số lượng/chỉ số +/-)
**Cờ cảnh báo mục 8.2:** Không.

---

### US-16: Hàng đợi cục bộ chống mất lượt scan khi mất mạng/crash
**Là** Công nhân vận hành trạm / Hệ thống
**Tôi muốn** mỗi lượt scan được ghi vào hàng đợi cục bộ trước khi gửi server, và tự động thử gửi lại nếu thất bại
**Để** không mất bất kỳ lượt scan nào kể cả khi mất mạng, server tạm ngưng, hoặc crash/mất điện đột ngột

**Acceptance Criteria**
- **AC1 — Ghi vào hàng đợi cục bộ trước tiên**
  - Given công nhân scan 1 tem
  - When hệ thống xử lý lượt scan
  - Then ghi ngay vào hàng đợi cục bộ (SQLite) ở trạng thái `Chờ gửi`, kèm 1 GUID sinh tại client, trước khi gửi lên server
- **AC2 — Gửi thành công, cập nhật trạng thái Đã đồng bộ**
  - Given lượt scan đã ghi vào hàng đợi cục bộ
  - When server phản hồi thành công
  - Then trạng thái cục bộ chuyển thành `Đã đồng bộ`, hiển thị kết quả OK/NG chính thức
- **AC3 — Không gửi được, giữ trạng thái Chờ gửi** *(AC-06 gốc)*
  - Given mất kết nối mạng giữa trạm và server trong lúc scan
  - When gửi lượt scan lên server không thành công (mất mạng/server tạm ngưng/timeout)
  - Then ứng dụng báo lỗi rõ ràng, không ghi nhận lượt scan "treo"; giữ nguyên `Chờ gửi`, hiển thị "Chờ đồng bộ"; tự kết nối lại khi mạng phục hồi *(AC-06)*
- **AC4 — Thử lại tự động định kỳ** *(AC-21 gốc)*
  - Given có nhiều bản ghi ở trạng thái `Chờ gửi` do mất mạng/server tạm ngưng
  - When tiến trình nền định kỳ (vd mỗi 5 giây) quét hàng đợi cục bộ
  - Then không chặn scan; tiếp tục thử gửi lại định kỳ theo đúng thứ tự thời gian đã scan; khi kết nối phục hồi, toàn bộ bản ghi tồn đọng tự động đồng bộ, không cần thao tác thủ công *(AC-21)*
- **AC5 — Khôi phục sau crash/mất điện, gửi lại không cần scan lại** *(AC-10 gốc)*
  - Given ngắt mạng/rút điện đột ngột ngay sau khi công nhân scan 1 tem (trước khi có phản hồi từ server), sau đó khởi động lại ứng dụng
  - When ứng dụng đọc lại hàng đợi cục bộ
  - Then lượt scan đó vẫn còn ở trạng thái "Chờ đồng bộ", tự động gửi lại lên server khi có mạng, không yêu cầu scan lại tem, và không bị ghi nhận trùng 2 lần trên server *(AC-10)*
- **AC6 — Chống gửi trùng (idempotency) qua GUID**
  - Given client gửi lại 1 lượt scan có GUID đã tồn tại (do gửi lặp vì crash giữa lúc chờ phản hồi)
  - When server nhận lượt gửi này
  - Then server trả về kết quả đã ghi nhận trước đó, không báo lỗi "trùng tem"
- **AC7 — Áp dụng đồng nhất cho mọi trạm/công đoạn**
  - Given hệ thống gồm nhiều trạm với công đoạn khác nhau
  - When triển khai cơ chế hàng đợi cục bộ
  - Then áp dụng như nhau cho mọi trạm, không phân biệt trạm nào đang là "công đoạn cuối chuỗi" (vì trình tự công đoạn có thể thay đổi theo kế hoạch)
- **AC8 — Không có cơ chế cảnh báo ngưỡng hay báo cáo đối soát riêng** (đã rút gọn theo mục 8.1)
  - Given cơ chế hàng đợi cục bộ + retry + idempotency đã đủ xử lý gián đoạn ngắn hạn
  - When thiết kế/triển khai FR-16
  - Then không xây dựng cơ chế cảnh báo theo ngưỡng số bản ghi tồn đọng/thời gian mất kết nối, và không tạo báo cáo đối soát riêng cho giai đoạn mất kết nối

**Nguồn FR:** FR-16, mục 6 quy tắc 8
**Phụ thuộc:** US-07, US-08 (cần có luồng scan cơ bản để gắn hàng đợi cục bộ vào)
**Cờ cảnh báo mục 8.2:** Không trực tiếp (đã rút gọn và chốt phạm vi rõ ràng trong SRS).

---

### US-17: Hiển thị rõ ràng trạng thái đồng bộ trên UI
**Là** Công nhân vận hành trạm / Tổ trưởng
**Tôi muốn** phân biệt rõ 3 trạng thái đồng bộ của mỗi lượt scan bằng màu sắc
**Để** biết chắc lượt nào đã được server xác nhận chính thức, tránh nhầm lẫn khi tính sản lượng

**Acceptance Criteria**
- **AC1 — Phân biệt 3 trạng thái bằng màu**
  - Given danh sách/số lượng lượt scan tại màn hình trạm
  - When hiển thị dữ liệu
  - Then phân biệt rõ 3 trạng thái: `Đã xác nhận OK` (xanh), `Đã xác nhận NG` (đỏ), `Chờ đồng bộ` (vàng/xám)
- **AC2 — Không tính lượt Chờ đồng bộ vào sản lượng chính thức**
  - Given có lượt scan ở trạng thái `Chờ đồng bộ`
  - When hiển thị số lượng/chỉ số +/-
  - Then lượt này chưa được tính vào sản lượng chính thức cho tới khi server xác nhận

**Nguồn FR:** FR-17
**Phụ thuộc:** US-16 (hàng đợi cục bộ — nguồn dữ liệu trạng thái đồng bộ)
**Cờ cảnh báo mục 8.2:** Không.

---

## 3.6 Nhóm chức năng: Scan NG & Quy trình Rework

### US-18: Scan xác nhận sản phẩm NG (chất lượng)
**Là** Công nhân vận hành trạm / Tổ trưởng
**Tôi muốn** chủ động chuyển sang Chế độ Scan NG, quét tem sản phẩm lỗi và nhập lý do
**Để** ghi nhận đúng sản phẩm không đạt chất lượng, khóa lại tại công đoạn đó cho tới khi được xử lý

**Acceptance Criteria**
- **AC1 — Kích hoạt Chế độ Scan NG bằng nút bấm yêu cầu đăng nhập Tổ trưởng** *(AC-13b gốc, cách 1 — SỬA 18/08/2026, xem Ghi chú "thay đổi yêu cầu")*
  - Given trạm có màn hình cảm ứng/chuột, chưa ở Chế độ Scan NG
  - When công nhân bấm nút "NG" (đỏ, lớn)
  - Then hệ thống hiển thị popup đăng nhập Tổ trưởng NGAY LẬP TỨC — CHƯA đổi giao diện nền đỏ, CHƯA vào Chế độ Scan NG cho tới khi đăng nhập thành công (AC2a)
- **AC2 — Kích hoạt Chế độ Scan NG bằng mã vạch cố định yêu cầu đăng nhập Tổ trưởng** (cách 2, cùng nội dung AC1 áp dụng cho trạm chỉ có đầu đọc mã vạch — SỬA 18/08/2026)
  - Given trạm chỉ có đầu đọc mã vạch, không có chuột/bàn phím, chưa ở Chế độ Scan NG
  - When công nhân quét mã vạch "NG" cố định dán tại bàn thao tác
  - Then hệ thống yêu cầu đăng nhập Tổ trưởng tương tự AC1
- **AC2a (mới) — Đăng nhập thành công và có quyền → vào Chế độ Scan NG**
  - Given popup đăng nhập đang hiển thị (theo AC1/AC2)
  - When tài khoản Supervisor/Admin/Manager đăng nhập đúng mật khẩu và có quyền `Scan.ConfirmNg`
  - Then giao diện đổi rõ rệt (nền đỏ, thông báo lớn "ĐANG Ở CHẾ ĐỘ NG"), bắt đầu đếm timeout 30 giây (AC7), chờ quét tem sản phẩm lỗi; hệ thống ghi nhớ tài khoản vừa đăng nhập cho tới khi lượt Scan NG này hoàn tất/bị hủy/timeout — KHÔNG yêu cầu đăng nhập thêm lần nào nữa trong suốt phần còn lại của lượt Scan NG này (quét tem, nhập lý do, bấm "Xác nhận NG" ở AC3/AC5 dùng lại đúng tài khoản đã đăng nhập ở đây)
- **AC2b (mới) — Đăng nhập sai hoặc tài khoản không có quyền xác nhận NG**
  - Given popup đăng nhập đang hiển thị (theo AC1/AC2)
  - When đăng nhập sai tài khoản/mật khẩu, HOẶC đăng nhập đúng nhưng tài khoản không có quyền `Scan.ConfirmNg`
  - Then hiển thị lỗi tương ứng ngay tại popup, KHÔNG vào Chế độ Scan NG, cho phép thử đăng nhập lại hoặc hủy
- **AC2c (mới) — Hủy đăng nhập**
  - Given popup đăng nhập đang hiển thị (theo AC1/AC2)
  - When người dùng bấm "Hủy"
  - Then đóng popup, KHÔNG vào Chế độ Scan NG, quay về Scan OK bình thường như chưa bấm nút "NG"/quét mã "NG"
- **AC3 — Scan tem lỗi và bắt buộc nhập lý do (không cần đăng nhập lại)** *(AC-13 gốc)*
  - Given đang ở Chế độ Scan NG (đã đăng nhập thành công ở AC2a)
  - When người vận hành quét tem sản phẩm lỗi và chọn/nhập lý do lỗi
  - Then hiển thị mã tem + lý do đã nhập, chờ bấm "Xác nhận NG"; KHÔNG yêu cầu đăng nhập lại ở bước này *(AC-13)*
- **AC4 — Nhập lý do dạng free text có gợi ý autocomplete**
  - Given công đoạn hiện tại đã từng có các lý do lỗi được nhập trước đó
  - When Tổ trưởng/công nhân gõ lý do lỗi mới
  - Then hệ thống gợi ý (autocomplete) các lý do đã từng nhập cho công đoạn này, không bắt buộc chọn từ danh mục cố định
- **AC5 — Ghi nhận đầy đủ thông tin lịch sử NG kèm người xác nhận** (SỬA 18/08/2026)
  - Given lượt Scan NG được xác nhận (bấm "Xác nhận NG" sau AC3/AC4)
  - When hệ thống lưu
  - Then lưu đủ: mã tem, công đoạn, trạm, thời gian, **người xác nhận = tài khoản đã đăng nhập ở AC2a (Id + tên hiển thị)**, lý do lỗi, kết quả = NG
- **AC6 — Tự động quay về Chế độ Scan OK sau khi hoàn tất** (điểm a, FR-18)
  - Given đã hoàn tất 1 lượt Scan NG (đã đăng nhập + quét tem + nhập lý do + xác nhận)
  - When lưu xong
  - Then trạm tự động quay về Chế độ Scan OK mặc định
- **AC7 — Tự động quay về Chế độ Scan OK khi hết timeout không quét tem** *(AC-13c gốc — SỬA 18/08/2026: mốc bắt đầu đếm)*
  - Given đã đăng nhập thành công (AC2a), đang ở Chế độ Scan NG chờ quét tem, nhưng không quét tem nào trong 30 giây
  - When hết thời gian timeout (mặc định 30 giây, cấu hình được qua file cục bộ)
  - Then tự động quay về Chế độ Scan OK mặc định, không ảnh hưởng lượt scan bình thường tiếp theo *(AC-13c)*. Thời gian hiển thị popup đăng nhập (AC1/AC2) KHÔNG tính vào 30 giây này — đồng hồ chỉ bắt đầu đếm SAU khi đăng nhập thành công (AC2a)

**Nguồn FR:** FR-18, mục 6 quy tắc 9 (yêu cầu đăng nhập Tổ trưởng ở AC1/AC2/AC2a-c là bổ sung 18/08/2026, ngoài phạm vi FR-18 gốc — xem Ghi chú)
**Phụ thuộc:** US-07, US-08 (luồng scan cơ bản), US-04 (cấu hình trạm — xác định trạm dùng nút bấm hay mã vạch NG), US-09 (Andon board tiêu thụ dữ liệu `Result = ScanResult.Ng` do US-18 tạo ra để tính ô NG/%NG — khi implement US-18 (bổ sung giá trị `Ng` vào enum `ScanResult`), PHẢI quay lại cập nhật `AndonBoardService.NgCount`/`NgPercent` để đếm đúng theo `ScanResult.Ng`, thay cho giá trị tạm `0/0%` đang để ở US-09), **ADR-005** (luồng đăng nhập Bearer + Refresh Token cho Supervisor nâng quyền tại trạm — AC1/AC2/AC2a-c nay tái dùng đúng luồng này, giống US-05/05a/05b), **ADR-004** (permission động — quyền `Scan.ConfirmNg` seed cho role Supervisor/Admin/Manager)
**Cờ cảnh báo mục 8.2:** Không trực tiếp (cách kích hoạt và free-text đã chốt trong mục 8.1).
**Ghi chú (18/08/2026, implement):** Các quyết định kỹ thuật tự đưa ra khi code (không có sẵn trong SRS/backlog gốc, ghi lại theo yêu cầu CLAUDE.md):
- **Mã vạch "NG" cố định (AC2)**: chọn literal chuỗi `"NG"` (so khớp chính xác, case-sensitive, sau khi `Trim()`) — không có trong SRS. Rủi ro chấp nhận được vì tem sản phẩm thực tế không đặt mã "NG". Hằng số `AndonBoardViewModel.NgModeActivationBarcode`.
- **AC5 "người xác nhận"**: xác nhận đây KHÔNG yêu cầu đăng nhập Supervisor/Operator — khác luồng Arduino (FR-12, yêu cầu Tổ trưởng đăng nhập xác nhận NG suy luận tự động), FR-18/AC1-AC7 chỉ mô tả kích hoạt bằng nút bấm/mã vạch tại trạm, không có bước đăng nhập nào. Quyết định: "người xác nhận" = `Scan.WorkStationId` (đã có sẵn) — KHÔNG thêm field `UserId` mới vào entity `Scan`, KHÔNG cần migration EF Core cho US-18 (đúng với comment sẵn có trong `Scan.cs`: "US-18/US-19 rework sau này sẽ dùng lại đúng bảng này").
- **AC3 (Scan NG không chạy 2 bước kiểm tra chống trùng/công đoạn liền trước của FR-08)**: xác nhận đây là hành động XÁC NHẬN CHỦ ĐỘNG của người vận hành, không phải luồng tự động — `ScanService.CreateNgAsync` luôn ghi `Result = Ng`, không kiểm tra `DuplicateTag`/`PreviousStageNotPassed`. "Tem bị khóa ở công đoạn kế tiếp" là hệ quả TỰ NHIÊN của việc công đoạn đó không có bản ghi `Ok` (logic `PreviousStageNotPassed` hiện có ở `ScanService.CreateAsync` đã đúng, không cần sửa) — có test xác nhận (`ScanServiceNgTests.SauKhiNg_TemBiKhoaKhiScanSangCongDoanKeTiep`).
- **%NG (gap US-09→US-18)**: đổi công thức từ `NgCount/TotalScanCount` (sai, đếm cả DuplicateTag/PreviousStageNotPassed) sang `NgCount/(OkCount+NgCount)` — chỉ tính trên các lượt scan phản ánh KẾT QUẢ SẢN PHẨM thật (đạt/không đạt), loại lỗi thao tác khỏi cả tử số lẫn mẫu số. `AndonBoardService.NgCount`/`NgPercent` đã sửa, `AndonBoardServiceTests` cập nhật theo.
- **Gợi ý lý do NG (AC4)**: sắp theo lần dùng gần nhất trước (không phải theo tần suất) — lý do mới nhập gần đây nhiều khả năng liên quan vấn đề chất lượng đang xảy ra hơn lý do cũ; giới hạn 20 lý do (`ScanService.MaxNgReasonSuggestions`).
- **UI Station.Wpf đặt ở `AndonBoardWindow`, KHÔNG phải `MainWindow`** (khác hướng dẫn ban đầu của người giao việc) — quyết định theo đúng ADR-006: `AndonBoardWindow` là nơi Operator tương tác trực tiếp (máy quét HID, `ScanInputBox`), Chế độ Scan NG (nút bấm + mã vạch, không đăng nhập) thuộc đúng ngữ cảnh đó; `MainWindow` chỉ dành cho Tổ trưởng đăng nhập cấu hình kế hoạch (US-05/05a/05b), không liên quan luồng scan. Đã ghi rõ deviation này để người review nắm.
- **Backend**: `ScanResult.Ng = 3`; `IScanService.CreateNgAsync`/`GetNgReasonSuggestionsAsync` mới; endpoint `POST api/v1/scans/ng` + `GET api/v1/scans/ng-reasons?stageId=` (`ScansController`, cùng scheme `StationApiKey`/ADR-005 như `POST api/v1/scans`); validator `CreateNgScanRequestValidator` (bắt buộc `RejectionReason`). KHÔNG có migration EF Core mới (không đổi entity `Scan`).
- **`Station.Wpf`**: `AndonBoardViewModel` thêm state máy NG mode (`IsNgModeActive`/`IsNgReasonPanelVisible`/`PendingNgTagCode`/`NgReasonText`/`NgReasonSuggestions`) + `_ngModeTimeoutTimer` (dùng `StationOptions.NgModeTimeoutSeconds`, AC7) + `ActivateNgModeCommand`/`ConfirmNgReasonCommand`/`CancelNgReasonCommand`; `HandleScanAsync` route theo state (mã "NG" → activate, đang chờ tem lỗi → mở form lý do, đang chờ lý do → bỏ qua tem thêm). `AndonBoardWindow.xaml` thêm nút "NG" (đỏ, lớn, Row 0) + overlay đỏ toàn màn hình (`Grid.RowSpan="3"`) khi `IsNgModeActive`, chứa form nhập lý do (`ComboBox` editable + `ItemsSource` gợi ý, AC4) khi đã quét tem lỗi. `Esc` khi `IsNgModeActive` hủy về Scan OK trước (đúng ADR-006, 1 chiều) thay vì chuyển `MainWindow`. Chưa có hàng đợi cục bộ SQLite (US-16 vẫn ⬜ chưa làm — Scan NG hiện gọi API trực tiếp giống luồng OK hiện có, cùng mức "chưa có" với US-07/US-08, không phải thiếu sót riêng của US-18).
- **Build/test**: `dotnet build ProductionMES.sln` sạch (0 Warning/0 Error, kể cả `ProductionMES.Api`); `dotnet test tests/ProductionMES.Application.Tests` 158/158 pass (9 test mới `ScanServiceNgTests`, cập nhật `AndonBoardServiceTests` cho %NG mới). **UI `Station.Wpf` CHƯA chạy thử bằng mắt** trên app Windows thật (không có công cụ chạy/chụp màn hình GUI trong phiên này) — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` với API + API Key trạm thật để xác nhận nút NG/overlay đỏ/form nhập lý do/autocomplete/timeout 30s hoạt động đúng trên máy thật trước khi coi UI đã xác nhận trực quan.
- **BUG phát hiện + fix 18/08/2026 (sau khi người dùng chạy thử thật)**: nút "NG" bấm không phản hồi gì. Nguyên nhân: `AndonBoardWindow.xaml.cs` → `ScanInputBox_LostFocus` cướp lại keyboard focus về `ScanInputBox` (ô ẩn bắt máy quét HID) NGAY GIỮA lúc đang xử lý 1 lượt click chuột (`Dispatcher.BeginInvoke(..., DispatcherPriority.Background)` có thể chạy xen giữa MouseDown và MouseUp — 2 message Win32 riêng biệt), khiến WPF hủy luôn Click/Command đang chờ xử lý của Button đó — không throw exception, không log, biểu hiện y hệt "click không phản hồi". Trước đó chỉ có `ManualScanInputBox`/`IsNgReasonPanelVisible` được miễn trừ khỏi cơ chế cướp focus này; nút "NG" (mới thêm ở US-18) thì chưa. **Fix**: đổi điều kiện miễn trừ thành "bất kỳ control tương tác nào" (`ButtonBase`/`ComboBox`/`TextBox`) thay vì liệt kê từng trường hợp riêng lẻ; các control đó tự chịu trách nhiệm trả lại focus cho `ScanInputBox` sau khi hoàn tất (qua `PropertyChanged` handler theo dõi `IsNgReasonPanelVisible`/`IsWaitingForNgTagCode`, xem code). Đã build sạch + verify trực tiếp trên máy thật (xác nhận qua debug MessageBox tạm thời, đã gỡ bỏ) rằng Click/Command chạy đúng và overlay đỏ hiển thị.

**Ghi chú (18/08/2026, thay đổi yêu cầu — ĐẢO NGƯỢC quyết định "không cần đăng nhập" ở Ghi chú "implement" phía trên):** Người giao việc yêu cầu bổ sung: bấm nút "NG"/quét mã "NG" phải đăng nhập Tổ trưởng NGAY LẬP TỨC (chặn từ đầu, không phải ở bước "Xác nhận NG" cuối cùng như mô tả gốc ban đầu của người giao việc — đã hỏi lại và chốt rõ qua AskUserQuestion), kiểu **re-auth mỗi lần** (không tái dùng session/token cũ dù còn hạn — khác hẳn pattern session-based đang dùng ở US-05/05a/05b `HomePage.RequireAuth`), và chỉ đăng nhập **1 lần duy nhất** cho toàn bộ 1 lượt Scan NG (không phải đăng nhập lại lần 2 ở bước "Xác nhận NG"). Quyền `Scan.ConfirmNg` (mới, theo ADR-004) seed cho **Supervisor + Admin + Manager** (rộng hơn chỉ Supervisor). Đã cập nhật AC1/AC2 (thêm bước đăng nhập chặn từ đầu), AC2a/AC2b/AC2c (mới — kết quả đăng nhập), AC3 (bỏ ghi "ghi nhận NG kèm lý do" ở bước quét tem, chuyển hẳn việc lưu sang sau AC5/bấm Xác nhận), AC5 (người xác nhận = tài khoản đăng nhập ở AC2a, không phải `WorkStationId`), AC7 (làm rõ mốc bắt đầu đếm 30s là SAU khi đăng nhập thành công, không tính thời gian hiển thị popup đăng nhập).

Phân tích chi tiết + các phương án đã cân nhắc (session vs re-auth, chặn đầu vs chặn cuối, phạm vi role) do agent `ba` thực hiện — xem lịch sử hội thoại nếu cần đối chiếu lại lý do. Phạm vi kỹ thuật cần `dev` triển khai lại (CHƯA làm, cần task riêng):
- **Domain**: `Scan.cs` thêm field `ConfirmedByUserId int?`/`ConfirmedByUserName string?` (nullable, KHÔNG backfill dữ liệu `Ng` cũ đã có — chấp nhận thiếu thông tin người xác nhận cho các bản ghi trước thay đổi này); sửa lại comment hiện đang giải thích lý do KHÔNG có `UserId` (không còn đúng nữa). `PermissionAction` thêm `ConfirmNg`.
- **Infrastructure**: migration EF Core mới cho `Scan` (cột mới); `DbSeeder` seed permission `Scan.ConfirmNg` cho `Supervisor`/`Admin`/`Manager`.
- **Application**: `ScanService.CreateNgAsync` nhận thêm định danh người xác nhận (từ claim token Bearer, không phải tham số DTO từ client); cập nhật `ScanServiceNgTests`.
- **Api**: `ScansController` action tạo Scan NG (`POST api/v1/scans/ng`) đổi/thêm authentication scheme — hiện dùng `StationApiKey` (ADR-005, không có `User` gắn với request), cần xác định `WorkStationId` (từ StationApiKey) VÀ danh tính Tổ trưởng (từ Bearer) CÙNG LÚC trong 1 request — cân nhắc: (a) 2 request riêng (1 request đăng nhập lấy token ngắn hạn dùng riêng cho lượt NG này, 1 request tạo Scan NG kèm token đó qua StationApiKey như cũ), hoặc (b) API action chấp nhận đồng thời 2 scheme (StationApiKey cho trạm + Bearer cho người xác nhận) — quyết định kỹ thuật cụ thể để `dev` tự chọn theo convention ASP.NET Core multi-scheme hiện có, miễn giữ đúng: trạm KHÔNG cần biết mật khẩu Tổ trưởng, chỉ cần token đã xác thực.
- **Station.Wpf**: `AndonBoardViewModel`/`AndonBoardWindow` chèn bước gọi `LoginDialog`/`ISupervisorAuthService` (tái dùng nguyên trạng luồng đã có ở `HomePage.RequireAuth`, KHÔNG tái dùng phần "session còn hạn thì bỏ qua" vì yêu cầu là re-auth mỗi lần) ngay tại `ActivateNgModeCommand`, TRƯỚC khi set `IsNgModeActive = true`; timer `_ngModeTimeoutTimer` (AC7) chỉ bắt đầu SAU khi đăng nhập thành công.

**Ghi chú (18/08/2026, dev triển khai lại — ĐÃ CODE, xem thêm chi tiết trong bảng "TRẠNG THÁI TRIỂN KHAI" đầu file):** Quyết định kỹ thuật cụ thể cho từng điểm mở ở trên:
- **Domain**: `Scan.ConfirmedByUserId int?`/`ConfirmedByUserName string?` — nullable, không backfill dữ liệu `Ng` cũ, comment cũ giải thích lý do KHÔNG có `UserId` đã sửa lại đúng theo thiết kế mới. `PermissionAction.ConfirmNg = 9`. Migration `AddScanConfirmedByFields` (EF Core, `int` nullable + `varchar(100)` nullable khớp `User.Username`).
- **Infrastructure**: `DbSeeder` — thêm `(Scan, ConfirmNg)` vào catalog `SeedPermissionsAsync` (Admin/Supervisor/Manager đều nhận qua nhánh `newPermissions` hiện có, vì đây là cặp Resource+Action HOÀN TOÀN MỚI, không giống tình huống gap của `Scan.View`/US-10); vẫn thêm `EnsureScanConfirmNgPermissionGrantsAsync` riêng làm lưới an toàn (cùng idiom `EnsureScanViewPermissionGrantsAsync`) dù về lý thuyết không bắt buộc phải có cho lần seed đầu.
- **Application**: `IScanService.CreateNgAsync`/`ScanService.CreateNgAsync` thêm tham số `confirmedByUserId`/`confirmedByUserName` (validate not-null/not-empty, ném `BusinessRuleException` nếu thiếu — phòng Service bị gọi trực tiếp không qua Controller); `BuildScan`/`ToDto`/`ToHistoryItemDto` map 2 field mới; `ScanResultDto`/`ScanHistoryItemDto` thêm field tương ứng (phục vụ US-19/US-20 sau này). `ScanServiceNgTests` cập nhật toàn bộ call site + thêm test `CreateNgAsync_ThieuThongTinNguoiXacNhan_NemBusinessRuleException` (4 case Theory) và assert `ConfirmedByUserId`/`ConfirmedByUserName` trong test hợp lệ.
- **Api — quyết định (a) đã chọn, KHÔNG chọn (b)**: TÁCH `POST api/v1/scans/ng` sang Controller mới `ScanNgController` (route `api/v1/scans/ng`, KHÔNG còn ở `ScansController`) — dùng scheme Bearer MẶC ĐỊNH (không khai `AuthenticationSchemes` tường minh, giống `ProductionPlanStagesController`) + `[Authorize(Policy = PermissionPolicies.ScanConfirmNg)]`. Lý do không chọn multi-scheme trên 1 action: ASP.NET Core merge nhiều `[Authorize]` bằng `AuthorizationPolicy.Combine()` (union `AuthenticationSchemes`, cộng dồn `Requirements`) — hành vi thực tế là "thử xác thực theo TỪNG scheme, gộp mọi claim của mọi scheme thành công vào 1 `ClaimsPrincipal`", KHÔNG phải "bắt buộc CẢ 2 scheme cùng thành công" như cần — rủi ro cao, khó verify đúng mà không chạy thử tay trên server thật. Tách Controller loại bỏ hẳn rủi ro này, đơn giản hơn để review. `WorkStationId` chuyển hẳn sang lấy từ `CreateNgScanRequest.WorkStationId` (request body) — Station.Wpf tự gán từ `StationOptions.WorkStationId` cục bộ, không expose UI gõ tay giá trị này nên rủi ro giả mạo chấp nhận được (khác hẳn mật khẩu/API Key — không phải bí mật cần bảo vệ). Người xác nhận (`ConfirmedByUserId`/`ConfirmedByUserName`) lấy từ claim `ClaimTypes.NameIdentifier`/`ClaimTypes.Name` của JWT — dùng **Username** làm `ConfirmedByUserName` (KHÔNG phải FullName): JWT hiện chỉ có claim Username, thêm claim FullName mới đòi Controller reference thẳng `ProductionMES.Infrastructure` (`JwtTokenGenerator`), vi phạm luật layer "Controller không được gọi thẳng Infrastructure" (CLAUDE.md) — chấp nhận Username, đủ để truy vết đúng tài khoản.
- **Station.Wpf**: `ISupervisorAuthService.LoginForNgConfirmationAsync` — gọi CHUNG endpoint `station-login` như `LoginAsync` nhưng KHÔNG gọi `ISupervisorSessionService.SetSession` (khác hẳn `LoginAsync`), trả thẳng `StationLoginResponse` cho caller tự giữ. `LoginDialogViewModel` thêm `RequiredPermission`/`NgConfirmationLoginResult`: khi `RequiredPermission != null`, `LoginCommand` gọi `LoginForNgConfirmationAsync` thay vì `LoginAsync`, kiểm tra `Permissions.Contains(RequiredPermission)` (danh sách "Resource.Action" đã có sẵn trong response đăng nhập, không cần round-trip riêng) — thiếu quyền thì set `ErrorMessage` và KHÔNG đóng dialog (AC2b, người dùng có thể thử lại hoặc bấm Hủy). `LoginDialog` expose `public LoginDialogViewModel ViewModel` để `AndonBoardViewModel` cấu hình trước/đọc kết quả sau `ShowDialog()`. `AndonBoardViewModel.ActivateNgModeCommand` (đổi `void` → `Task`, CommunityToolkit tự strip hậu tố `Async` khi generate command nên binding XAML `ActivateNgModeCommand` không đổi) gọi `AuthenticateForNgMode()` (đồng bộ, `ShowDialog()` chặn UI thread giống `HomePage.RequireAuth`) TRƯỚC khi set `IsNgModeActive = true`; owner của dialog = `Application.Current?.MainWindow` (WPF tự gán `MainWindow` = cửa sổ đầu tiên `Show()`, ở đây là `AndonBoardWindow`, do `AndonBoardViewModel` không giữ tham chiếu trực tiếp tới `View` của chính nó). Token đăng nhập lưu ở field riêng `_ngScanAccessToken` (KHÔNG set vào `ISupervisorSessionService`), bị xoá vô điều kiện trong `DeactivateNgMode()` (chạy ở cả 3 nhánh: hoàn tất `ConfirmNgReasonAsync`, hủy `CancelNgReasonCommand`, timeout `_ngModeTimeoutTimer`) — đảm bảo đúng 1 lần đăng nhập/lượt, không rò rỉ sang lượt Scan NG kế tiếp; nhánh "đã active, chưa quét tem, chỉ rescan mã NG để gia hạn timeout" (AC7 "quét lại mã NG để gia hạn") vẫn giữ nguyên KHÔNG đăng nhập lại (kiểm tra `IsNgModeActive && PendingNgTagCode is null` trước khi gọi `AuthenticateForNgMode`). `IScanApiClient.CreateNgAsync` thêm tham số `supervisorAccessToken`, `ScanApiClient` tự gắn `Authorization: Bearer` trực tiếp vào `HttpRequestMessage` (không đổi `HttpClient`/handler đăng ký trong `App.xaml.cs` — `StationApiKeyHandler` vẫn gắn `X-Station-Api-Key` cho `CreateAsync`/`GetNgReasonSuggestionsAsync` của cùng client, header đó bị `ScanNgController` bỏ qua vô hại vì Controller không khai `AuthenticationSchemes = StationApiKey`); 401 từ endpoint này được bắt riêng và đổi message (khác `UnauthorizedMessage` mặc định của `ScanApiClient`, vốn nói về StationApiKey — không đúng ngữ cảnh ở đây). **Không revoke refresh token sau khi dùng xong** (chấp nhận được — nhất quán với hành vi hiện có: session US-05/05a/05b cũng không tự revoke khi đóng app, access token 15 phút tự hết hạn).
- **Build/test**: `dotnet build ProductionMES.sln` sạch (0 Warning/0 Error, kể cả `Station.Wpf`); `dotnet test tests/ProductionMES.Application.Tests` 162/162 pass. **UI CHƯA chạy thử bằng mắt trên máy thật** — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` để xác nhận AC1/AC2/AC2a/AC2b/AC2c/AC7 trước khi coi phần này đã xác nhận trực quan.

---

### US-19: Quy trình Rework (mở khóa & scan lại sau NG)
**Là** Tổ trưởng
**Tôi muốn** mở khóa rework cho tem đã bị NG sau khi xác nhận sản phẩm đã được sửa lỗi
**Để** cho phép công nhân scan lại tem đó tại đúng công đoạn, đồng thời giữ lại toàn bộ lịch sử các lần scan phục vụ báo cáo

**Acceptance Criteria**
- **AC1 — Không tự động scan lại khi chưa mở khóa** *(AC-14 gốc)*
  - Given tem vừa bị NG, chưa được Tổ trưởng mở khóa
  - When công nhân cố gắng tự scan lại tem tại cùng công đoạn
  - Then hệ thống từ chối, báo "Sản phẩm đang chờ mở khóa rework" *(AC-14)*
- **AC2 — Mở khóa rework bởi Tổ trưởng**
  - Given tem đang bị khóa do NG tại 1 công đoạn
  - When Tổ trưởng (tài khoản có phân quyền riêng) xác nhận đã sửa lỗi và thực hiện "Mở khóa rework"
  - Then tem được mở khóa, ghi log thao tác (ai duyệt, thời điểm, ghi chú nếu có)
- **AC3 — Scan lại sau khi mở khóa, đạt** *(AC-15 gốc)*
  - Given Tổ trưởng đã mở khóa rework cho tem
  - When công nhân scan lại và đạt
  - Then ghi nhận thêm 1 bản ghi OK mới (không ghi đè bản ghi NG cũ), tem được phép sang công đoạn kế tiếp *(AC-15)*
- **AC4 — Scan lại vẫn không đạt, lặp lại quy trình**
  - Given tem đã mở khóa và scan lại nhưng vẫn NG
  - When công nhân/Tổ trưởng xác nhận NG lần nữa
  - Then tiếp tục quy trình NG/mở khóa như trên, không giới hạn số lần lặp lại
- **AC5 — Giữ toàn bộ lịch sử, không ghi đè**
  - Given tem đã trải qua nhiều lần NG/OK tại cùng 1 công đoạn
  - When truy vấn lịch sử
  - Then tất cả các lần scan (NG lẫn OK) đều được lưu lại đầy đủ, không có bản ghi nào bị ghi đè/xóa
- **AC6 — Chỉ Tổ trưởng có quyền mở khóa**
  - Given người dùng đang đăng nhập với vai trò Công nhân
  - When cố gắng thực hiện thao tác "Mở khóa rework"
  - Then hệ thống từ chối do không đủ quyền

**Nguồn FR:** FR-19, mục 6 quy tắc 9
**Phụ thuộc:** US-18 (Scan NG), US-22 (phân quyền — cần vai trò Tổ trưởng được định nghĩa trước khi kiểm soát quyền mở khóa)
**Cờ cảnh báo mục 8.2:** Không trực tiếp. Lưu ý kỹ thuật quan trọng đã nêu trong SRS (ràng buộc `UNIQUE(MaTem, CongDoanId)` phải đổi thành "tối đa 1 bản ghi OK", xử lý ở tầng Service — dev cần đọc kỹ mục 3.6 SRS khi thiết kế entity).

---

### US-20: Báo cáo tỷ lệ lỗi & nguyên nhân
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

## 3.7 Nhóm chức năng: Báo cáo & quản trị

### US-21: Báo cáo tổng hợp theo Line
**Là** Ban quản lý / Văn phòng
**Tôi muốn** xem sản lượng thực tế, kế hoạch, chỉ số âm/dương của từng Line, từng công đoạn
**Để** theo dõi tổng thể tình hình sản xuất toàn nhà máy theo thời gian thực hoặc theo khoảng thời gian đã qua

**Acceptance Criteria**
- **AC1 — Xem theo thời gian thực**
  - Given hệ thống đang có dữ liệu scan phát sinh
  - When người dùng mở màn hình báo cáo tổng hợp
  - Then thấy sản lượng thực tế, kế hoạch, chỉ số +/- của từng Line/công đoạn cập nhật theo thời gian thực
- **AC2 — Xem theo khoảng thời gian đã qua**
  - Given người dùng chọn khoảng thời gian trong quá khứ
  - When xem báo cáo
  - Then hiển thị đúng dữ liệu sản lượng/kế hoạch/chỉ số trong khoảng đó
- **AC3 — Chỉ tính lượt Đã xác nhận OK**
  - Given có lượt scan ở trạng thái `Chờ đồng bộ` chưa được server xác nhận
  - When tính toán báo cáo
  - Then báo cáo chỉ tính các lượt scan ở trạng thái `Đã xác nhận OK`, không tính lượt `Chờ đồng bộ` cho tới khi được xác nhận

**Nguồn FR:** FR-21
**Phụ thuộc:** US-09, US-16, US-17 (cần dữ liệu số lượng, chỉ số +/-, và phân biệt trạng thái đồng bộ đã có trước)
**Cờ cảnh báo mục 8.2:** Không trực tiếp.

---

### US-22: Quản lý người dùng & phân quyền
**Là** Quản trị hệ thống (Admin)
**Tôi muốn** quản lý tài khoản người dùng và phân quyền theo 4 nhóm vai trò
**Để** đảm bảo mỗi người dùng chỉ thực hiện đúng chức năng thuộc phạm vi vai trò của mình

**Acceptance Criteria**
- **AC1 — Phân quyền theo 4 nhóm vai trò**
  - Given hệ thống có 4 nhóm người dùng: Công nhân vận hành trạm, Tổ trưởng/Quản lý chuyền, Admin, Ban quản lý/Văn phòng
  - When Admin gán vai trò cho tài khoản
  - Then mỗi tài khoản chỉ truy cập được các chức năng đúng phạm vi vai trò tương ứng (mục 2.2 SRS)
- **AC2 — Quyền riêng cho thao tác Mở khóa rework**
  - Given tài khoản có vai trò Công nhân
  - When cố gắng thực hiện thao tác "Mở khóa rework" (FR-19)
  - Then hệ thống từ chối, chỉ tài khoản vai trò Tổ trưởng mới thực hiện được
- **AC3 — Không có vai trò QC riêng biệt**
  - Given hệ thống chỉ định nghĩa 4 vai trò theo mục 2.2 SRS
  - When cấu hình phân quyền
  - Then không tồn tại vai trò "QC" độc lập — mọi chức năng trước đây gắn với "QC" thuộc về vai trò Tổ trưởng

**Nguồn FR:** FR-22, mục 8.1 (quyết định gộp QC vào Tổ trưởng)
**Phụ thuộc:** Không có ràng buộc cứng về thứ tự dữ liệu nghiệp vụ, nhưng nên có sớm vì nhiều story khác (US-19, US-12) cần cơ chế phân quyền/đăng nhập Tổ trưởng để hoạt động đúng.
**Cờ cảnh báo mục 8.2:** Không.

---

### US-23: Xuất báo cáo Excel
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

## THỨ TỰ TRIỂN KHAI ĐỀ XUẤT

**Giai đoạn 1 — Dữ liệu nền tảng (danh mục & cấu hình)**
1. US-01 (Line)
2. US-01a (Khung giờ nghỉ theo Line) — cần US-01 xong trước; là điều kiện tiên quyết cho US-09 AC5/AC6
3. US-02 (Công đoạn master)
4. US-04 (Trạm làm việc) — cần Line + Công đoạn xong trước
5. US-04a (API Key theo trạm) — cần US-04 xong trước; là điều kiện tiên quyết bắt buộc cho US-07/US-08 (`Station.Wpf` không xác thực được nếu thiếu story này)
6. US-05 (Kế hoạch sản xuất — màn hình Cài đặt kế hoạch)
7. US-03 (Cấu hình trình tự công đoạn cho Line) — thực ra chỉ cần US-01/US-02 xong trước (không phụ thuộc US-05 nữa, xem sửa 17/08/2026), đặt sau US-05 trong danh sách này chỉ vì lý do lịch sử triển khai
8. US-05a (Vòng đời trạng thái kế hoạch theo từng công đoạn) — cần US-03 xong trước (cần biết công đoạn nào thuộc trình tự của Line để suy ra các cặp (Kế hoạch, Công đoạn) cần theo dõi)
9. US-05b (Chọn & áp dụng kế hoạch — màn hình Chọn kế hoạch) — cần US-05a xong trước
10. US-06 (Tính sản lượng chuẩn theo giờ)
11. US-22 (Quản lý người dùng & phân quyền)

*Lý do*: Đây là toàn bộ dữ liệu master mà mọi luồng nghiệp vụ phía sau (scan, Arduino, rework, báo cáo) đều phụ thuộc trực tiếp. US-01a và US-04a được chèn ngay sau story gốc của chúng (US-01/US-04) vì đây là 2 khoảng trống phát sinh sau khi US-01/US-04 đã code xong (FR-01/FR-09a bổ sung sau; ADR-005 chốt sau) — bắt buộc phải xong trước khi bước vào Giai đoạn 2, nếu không US-07/US-08/US-09 sẽ bị chặn giữa chừng. US-05a/US-05b tương tự được chèn ngay sau US-05/US-03 vì đây là khoảng trống phát sinh sau khi US-05 gốc đã lập (FR-05a bổ sung sau khi phân tích UI 13/08/2026) — US-07 (`Station.Wpf` lấy "kế hoạch active của trạm") cần US-05a đã có khái niệm `Running` theo (Line, Công đoạn) mới hoạt động đúng. Phân quyền (US-22) đặt sớm trong giai đoạn này vì US-12 (xác nhận Arduino) và US-19 (mở khóa rework) đều cần cơ chế đăng nhập/phân quyền Tổ trưởng để hoạt động đúng ngay từ khi build luồng lõi.

**Giai đoạn 2 — Luồng scan lõi (happy path)**
12. US-07 (Scan tem tại trạm)
13. US-08 (Kiểm tra hợp lệ khi scan — chống trùng tem, công đoạn liền trước)
14. US-09 (Hiển thị số lượng & chỉ số +/-)
15. US-10 (Lưu & tra cứu lịch sử scan)
16. US-15 (Khôi phục trạng thái phiên khi mở lại bình thường)

*Lý do*: Đây là lõi nghiệp vụ trung tâm của toàn hệ thống (scan → kiểm tra → cập nhật số liệu → lưu lịch sử). Cần hoàn thiện và ổn định trước khi thêm các nhánh phức tạp hơn (Arduino, offline, NG) vì các nhánh đó đều là biến thể/mở rộng của luồng scan cơ bản này.

**Giai đoạn 3 — Chống mất dữ liệu (offline/crash)**
17. US-16 (Hàng đợi cục bộ chống mất lượt scan)
18. US-17 (Hiển thị trạng thái đồng bộ trên UI)

*Lý do*: Đây là lớp bọc thêm quanh luồng scan lõi (ghi trước vào local queue, retry, idempotency) — cần scan lõi hoạt động ổn định trước, sau đó mới bọc thêm cơ chế an toàn dữ liệu này vì nó thay đổi cách trạm gửi/nhận kết quả scan.

**Giai đoạn 4 — Nhánh phụ: Scan NG & Rework**
19. US-18 (Scan xác nhận sản phẩm NG)
20. US-19 (Quy trình Rework — mở khóa & scan lại)
21. US-20 (Báo cáo tỷ lệ lỗi & nguyên nhân)

*Lý do*: NG là nhánh rẽ có điều kiện của luồng scan (không phải mọi lượt scan đều NG), và có ràng buộc dữ liệu phức tạp hơn (nhiều bản ghi tại cùng công đoạn, thay đổi ràng buộc unique) nên làm sau khi luồng OK cơ bản đã vững. Báo cáo tỷ lệ lỗi (US-20) đặt cuối nhóm này vì cần có dữ liệu NG/OK thực tế để thống kê.

**Giai đoạn 5 — Nhánh phụ: Arduino**
22. US-11 (Cấu hình bật/tắt Arduino theo trạm)
23. US-14 (Kết nối & phục hồi cổng COM)
24. US-13 (Timeout xác định kết quả kiểm tra)
25. US-12 (Luồng scan-chờ-Arduino đầy đủ, bao gồm nhánh xác nhận NG bởi Tổ trưởng)

*Lý do*: Arduino là nhánh phụ thuộc phần cứng đặc thù, chỉ áp dụng cho một số trạm/công đoạn nhất định (chưa xác định rõ theo mục 8.2). Đặt sau NG/Rework vì US-12 (bước 5) tái sử dụng trực tiếp cơ chế xác nhận NG + khóa/mở khóa rework đã xây ở Giai đoạn 4. Nên làm US-11/US-14 (cấu hình, kết nối) trước US-12/US-13 (logic state machine đầy đủ) vì đây là điều kiện kỹ thuật cần có trước khi build luồng nghiệp vụ chờ kết quả.

**Giai đoạn 6 — Báo cáo tổng hợp & xuất Excel**
26. US-21 (Báo cáo tổng hợp theo Line)
27. US-23 (Xuất báo cáo Excel)

*Lý do*: Báo cáo phụ thuộc vào dữ liệu đã tích lũy đầy đủ từ tất cả các luồng trước (scan OK, chỉ số +/-, trạng thái đồng bộ, dữ liệu NG). Xuất Excel (US-23) làm cuối cùng vì còn phụ thuộc thêm vào việc xác nhận nội dung/mẫu báo cáo cụ thể (điểm mở #4 mục 8.2 — cần hỏi lại stakeholder trước khi code phần này).

---

## GHI CHÚ CHUNG VỀ CÁC ĐIỂM CÒN MỞ (mục 8.2 SRS)

4 điểm sau đây chưa được khách hàng xác nhận, đã được gắn cờ cảnh báo tại từng story liên quan ở trên — dev cần hỏi lại trước khi triển khai phần liên quan, dù không chặn việc code phần khung/logic chung:

1. **Số lượng Line & danh sách công đoạn cụ thể từng Line** — ảnh hưởng US-01, US-03 (khi cấu hình dữ liệu thật, không ảnh hưởng thiết kế chức năng).
2. **Danh sách công đoạn cụ thể dùng Arduino** — ảnh hưởng US-02 (gắn cờ), US-04, US-11, US-12 (phạm vi trạm cần triển khai state machine Arduino).
3. **Model máy scan có đồng nhất Zebra DS2208 hay không** — ảnh hưởng US-04, US-07 (thiết kế giao tiếp HID có áp dụng chung cho mọi trạm hay cần xử lý riêng cho model khác).
4. **Nội dung cụ thể báo cáo Excel (cột dữ liệu, cách nhóm/tổng hợp)** — ảnh hưởng trực tiếp US-23, cần xác nhận trước khi thiết kế mẫu xuất file.
