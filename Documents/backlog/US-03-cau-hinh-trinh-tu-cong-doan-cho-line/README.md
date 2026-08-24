# US-03: Cấu hình trình tự công đoạn cho Line
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

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-17

## Lịch sử triển khai (ghi chú backlog)

**17/08 (xác nhận trực quan)**: người dùng đã tự chạy `Station.Wpf`, thử đầy đủ thao tác thêm/gỡ/sắp xếp trình tự công đoạn của Line tại `LineStageSequencePage` — hoạt động đúng như AC1-AC8. **17/08 (implement lại theo thiết kế mới)**: entity `LineStageSequence` (LineId/StageId/SequenceNumber, unique theo LineId+SequenceNumber và LineId+StageId) thay hẳn `ProductionPlanStage.SequenceNumber` (đã xoá property này khỏi entity). `LineStageSequenceService`/`ILineStageSequenceService` (AddAsync/RemoveAsync/ReorderAsync/GetByLineAsync) + `LineStageSequencesController` (`api/v1/lines/{lineId}/stage-sequence`, dùng lại permission `ProductionPlanStage.*`) cover đủ AC1-AC7. `ProductionPlanStageService` sửa lại cơ chế "lazy get-or-create": `GetByProductionPlanAsync`/`ApplyAsync`/`PauseAsync`/`CloseAsync` tự tạo bản ghi `PlanStatus=Draft` khi cần dựa theo trình tự Line, `GetByLineAndStageAsync` (US-05b) liệt kê mọi `ProductionPlan` của Line thay vì chỉ các row đã tồn tại; `AddAsync/RemoveAsync/ReorderAsync` cũ đã xoá khỏi service/controller này (chuyển hẳn sang `LineStageSequenceService`) — `IProductionPlanStageService.GetByProductionPlanAsync`/`ProductionPlanStageDto` giữ nguyên shape nên `ScanService`/`ScanServiceTests` KHÔNG phải sửa. UI `Station.Wpf`: bỏ khu vực "Công đoạn của kế hoạch" khỏi `PlanSettingsPage`/`PlanSettingsViewModel`, thêm màn mới `LineStageSequencePage`/`LineStageSequenceViewModel` (+ API client `ILineStageSequenceApiClient`), điều hướng từ `HomePage`/`MainWindow` theo ADR-006. Migration `AddLineStageSequence_RemoveProductionPlanStageSequenceNumber` đã generate VÀ apply thành công lên DB dev cục bộ (`dotnet ef database update` kết nối được MySQL thật). Solution build sạch (`dotnet build ProductionMES.sln`), 125/125 test pass (`dotnet test`, thêm mới `LineStageSequenceServiceTests`, sửa lại `ProductionPlanStageServiceTests`). Toàn bộ code chưa commit (giữ nguyên working tree theo yêu cầu) — UI chưa chạy thử bằng mắt (không có công cụ chụp màn hình Windows app trong phiên này), cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` để xác nhận trực quan. **17/08 (gap mới, cùng đợt)**: `LineStageSequencePage` cũng đang để `TextBox` gõ tay đổi `LineId` thay vì hiển thị Tên Line cố định readonly — đã bổ sung AC8, chưa implement. **17/08 (đã sửa AC8)**: bỏ hẳn `TextBox` gõ tay `LineId`, thay bằng `TextBlock` readonly hiển thị `LineName` (tra qua `ILineApiClient` mới, US-01) — Line vẫn cố định theo `StationOptions.LineId`, không cho đổi từ màn này. Cũng bổ sung seed permission `Line.View`/`Stage.View` cho role `Supervisor` trong `DbSeeder.EnsureSupervisorCatalogViewPermissionsAsync` (gap phát hiện thêm: `Stage.View` vốn đã cần cho combobox Công đoạn của chính màn này từ trước nhưng CHƯA từng được seed cho Supervisor — nếu không vá, `IStageApiClient.GetAllAsync()` sẽ luôn 403 với tài khoản Supervisor thật). Build `dotnet build src/ProductionMES.Station.Wpf` + `dotnet build src/ProductionMES.Infrastructure` pass, 125/125 test Application pass (không đổi Application layer). Api không build lại được trong phiên này do bị Visual Studio khoá file (đang chạy), chỉ build riêng từng project bị ảnh hưởng để xác nhận biên dịch đúng — UI vẫn CHƯA chạy thử bằng mắt trên app Windows thật
