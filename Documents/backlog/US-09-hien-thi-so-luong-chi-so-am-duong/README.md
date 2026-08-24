# US-09: Hiển thị số lượng và chỉ số âm/dương tại trạm
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

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần
- **Cập nhật:** 2026-08-17

## Lịch sử triển khai (ghi chú backlog)

**17/08**: Backend + UI `Station.Wpf` đã code xong đầy đủ AC1-AC6. Backend mới: `IAndonBoardService`/`AndonBoardService` (`src/ProductionMES.Application/Services/AndonBoard/`) — xác định "kế hoạch active của trạm" bằng đúng cách US-05a đã chốt (`ProductionPlanStage.PlanStatus = Running` theo cặp Line+Stage của trạm, giống `ScanService`), tính PLAN/ACTUAL/BALANCE/NG/%NG qua `AndonBoardCalculator` (static, thuần túy, tách riêng để test AC5/AC6 không cần mock DB) — công thức trừ khung giờ nghỉ (AC5/AC6) dùng cách "trừ phần giao [StartTime, at] với từng BreakWindow", tự nhiên thỏa "PLAN đứng lại khi đang trong giờ nghỉ" mà không cần if/else riêng. Endpoint mới `GET api/v1/andon-board` (`AndonBoardController`), cùng scheme `StationApiKey` (ADR-005) và cách lấy `WorkStationId` từ claim như `ScansController` — khác `ScanService`, endpoint này KHÔNG lỗi 409 khi (Line,Stage) chưa có kế hoạch Running (chỉ hiển thị, trả `HasActivePlan=false`). UI: `AndonBoardViewModel` thêm `Rows` (ObservableCollection), `PlanCumulative`/`Balance`/`NgCount`/`NgPercent`/`HasActivePlan`, tải qua `IAndonBoardApiClient` mới lúc `Window_Loaded` + làm mới định kỳ (`StationOptions.AndonBoardRefreshIntervalSeconds`, mặc định 30s, cho PLAN "trôi" theo thời gian và phát hiện mốc giờ mới — AC6), còn ACTUAL/BALANCE tăng tức thời (AC4, ≤1s) bằng cách tái dùng đúng sự kiện SignalR `ScanRecorded` đã có từ US-07 (cộng dồn client-side vào dòng "hiện tại", không gọi lại API). `AndonBoardWindow.xaml` thay placeholder bằng bảng thật (cột TIME/PLAN/ACTUAL/BALANCE, dòng hiện tại highlight nền xanh đậm) + 1 ô NG/%NG gộp bên cạnh, giữ nguyên nền tối/hex cục bộ theo ADR-007 (không dùng Light theme). Balance dương xanh (#4CAF50)/âm đỏ (#E53935) đúng AC2/AC3. **Gap cố ý theo đúng phạm vi giao việc**: ô "tên nhân viên" (PER) trong mockup gốc (`Documents/Layout WPF/Capture.PNG`) KHÔNG implement — entity `Scan` không có `UserId`/định danh operator (đúng thiết kế ADR-005, Operator không đăng nhập cá nhân ở luồng scan thường), cần BA quyết định nguồn dữ liệu trước khi làm, không tự chế. Các field header khác trong mockup (Model/Customer/Lot/Takt time hiển thị dạng chip, Starting/End time, đồng hồ ngày giờ, mã "8-YX0S") cũng KHÔNG thêm vì không nằm trong AC1-AC6 chính thức — tránh mở rộng ngoài phạm vi. **Giả định kỹ thuật cần lưu ý**: mọi phép tính giờ ở `AndonBoardCalculator`/`AndonBoardService` dùng giờ tường (wall-clock) tại máy chủ API (`DateTime.Now`), CÙNG quy ước "không quy đổi múi giờ" đã có sẵn từ US-05 với `ProductionPlan.StartTime` — giả định máy chủ API và nhà máy cùng múi giờ (hệ thống hiện chưa xử lý đa múi giờ ở bất kỳ đâu, không phải gap mới của US-09). Build `dotnet build ProductionMES.sln` sạch (0 lỗi biên dịch — riêng `ProductionMES.Api` bị lock file do `ProductionMES.Api.exe` đang chạy sẵn từ trước, chỉ lỗi copy MSB3027 ngoài tầm kiểm soát, không có `error CS` nào); 141/141 test Application pass (`dotnet test`, thêm mới `AndonBoardCalculatorTests` 11 test cho AC5/AC6 và `AndonBoardServiceTests` 5 test cho orchestration). Toàn bộ code CHƯA commit (giữ nguyên working tree). **CHƯA xác nhận trực quan bằng mắt** trên app Windows thật (không có công cụ chạy/chụp màn hình GUI trong phiên này) — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` với API + kế hoạch đang Running thật để xác nhận bảng hiển thị đúng, highlight dòng hiện tại, màu Balance, và AC4 (scan xong ACTUAL tăng ngay) trước khi đổi sang ✅ Xong. **17/08 (bổ sung header theo mockup gốc 13/08/2026 đã chốt với người dùng)**: thêm các field header trước đó bị thiếu — DATE/TIME đồng hồ realtime (client-side, `DispatcherTimer` mỗi giây, không gọi API), MODEL/LOT/PROD.PLAN/TAKT TIME (format phút:giây, tái dùng `TaktTimeFormat.ToDisplay`)/STARTING TIME/ô PER (`OperatorNames`) — map thẳng từ `ProductionPlan` đã load sẵn trong `AndonBoardService.GetForWorkStationAsync`, KHÔNG thêm field `EndTime` (không tồn tại trên entity, không có nguồn dữ liệu). Backend: `AndonBoardDto` thêm `Model`/`Lot`/`PlannedQuantity`/`TaktTimeSeconds`/`PlanStartTime`/`OperatorNames`, set trong `AndonBoardService` (chỉ khi `HasActivePlan=true`). `AndonBoardServiceTests` cập nhật assert field mới ở cả 2 nhánh có/không có kế hoạch active. `Station.Wpf`: mirror DTO, `AndonBoardViewModel` thêm property tương ứng + `CurrentDateLabel`/`CurrentTimeLabel` (đồng hồ), `AndonBoardWindow.xaml` thêm 3 dòng mới vào Row 0 (row-model, row-plan, row-shift) và đồng hồ DATE/TIME phía trên khối ACTUAL/PLAN/BALANCE cũ — **bảng PLAN/ACTUAL/BALANCE theo mốc giờ ở Row 1 giữ nguyên, không đổi**. Build `Application` + `Station.Wpf` riêng lẻ pass 0 lỗi (`ProductionMES.Api` vẫn bị Visual Studio khoá file, không phải lỗi biên dịch — 0 `error CS`); 141/141 test Application pass. Vẫn **CHƯA xác nhận trực quan** header mới trên app thật — giữ 🟡 **17/08 (gap NG/%NG, chờ US-18)**: rà soát lại phát hiện `AndonBoardService.NgCount` hiện tính bằng `scans.Count(s => s.Result != ScanResult.Ok)` — SAI, vì đếm cả `ScanResult.DuplicateTag`/`ScanResult.PreviousStageNotPassed` (lỗi thao tác operator), không phải lỗi chất lượng. Đã bổ sung ghi chú chéo vào mục Phụ thuộc của US-09 và US-18: nguồn đúng cho NG/%NG là `ScanResult.Ng` (US-18, chưa tồn tại) — tạm thời NG/%NG PHẢI trả về `0`/`0%` cho tới khi US-18 implement, lúc đó dev PHẢI quay lại sửa `AndonBoardService.NgCount`/`NgPercent`. **18/08 (gap đã sửa cùng US-18)**: `AndonBoardService.NgCount` nay đếm đúng `Result == ScanResult.Ng`; `NgPercent = NgCount/(OkCount+NgCount)*100` (loại bỏ DuplicateTag/PreviousStageNotPassed khỏi cả tử số lẫn mẫu số). `AndonBoardServiceTests` cập nhật theo. Chi tiết xem mục US-18.
