# US-10: Lưu và tra cứu lịch sử scan
**Là** Tổ trưởng / Admin / Ban quản lý
**Tôi muốn** mọi lượt scan (kể cả lỗi) được lưu lại và có thể tra cứu theo nhiều tiêu chí
**Để** phục vụ truy vết, đối chiếu, và làm nền tảng dữ liệu cho báo cáo sau này

**Acceptance Criteria**
- **AC1 — Lưu đầy đủ mọi lượt scan**
  - Given công nhân thực hiện 1 lượt scan (dù kết quả OK, Trùng tem, hay Chưa qua công đoạn trước)
  - When hệ thống xử lý
  - Then lượt scan được lưu lại với đầy đủ: mã tem, thời gian, trạm, công đoạn, Line, kế hoạch, kết quả, người thao tác — kèm **snapshot** Khách hàng/Model/Lot/Revision/Số lượng kế hoạch/Takt time/**Tên nhân viên vận hành (`OperatorNames`, bổ sung 19/08/2026)** của kế hoạch đó tại đúng thời điểm scan (không phải giá trị hiện tại của kế hoạch, tránh sai lệch nếu kế hoạch bị sửa Số lượng/Takt time/OperatorNames sau này — xem US-05 AC6)
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
- **AC5 — Snapshot `OperatorNames` không phải định danh cá nhân theo lượt scan (giới hạn nghiệp vụ, bổ sung 19/08/2026)**
  - Given `ProductionPlan.OperatorNames` là free-text khai báo CHUNG cho toàn bộ kế hoạch (không phân biệt theo từng công đoạn/`ProductionPlanStage`, xem US-05 mục "Tên nhân viên")
  - When tra cứu lịch sử scan (AC2/AC3) và xem field `OperatorNames` snapshot của 1 lượt scan
  - Then giá trị hiển thị là "danh sách người vận hành được khai báo cho kế hoạch tại thời điểm scan" — KHÔNG phải "người thực tế đã bấm scan lượt đó"; không diễn giải/sử dụng field này như 1 định danh audit cá nhân chính xác cho từng lượt scan (Operator không đăng nhập cá nhân, ADR-005) — xem mục 8.2 SRS

**Nguồn FR:** FR-10, mục 6 quy tắc 6, quy tắc 14
**Phụ thuộc:** US-07, US-08. AC4 (snapshot bất biến) nên triển khai **cùng đợt** với US-05 AC6 (khóa tuyệt đối Khách hàng/Model/Lot/Revision khi đã có scan) — cả 2 cùng xử lý 1 gap phát hiện ngày 14/08/2026, cùng chạm entity `Scan`/`ProductionPlanService`.
**Cờ cảnh báo mục 8.2:** Không.
**Ghi chú:** Cập nhật 14/08/2026 — AC1 bổ sung yêu cầu lưu snapshot 6 field (Customer/Model/Lot/Revision/PlannedQuantity/TaktTimeSeconds) vào `Scan` tại thời điểm scan; AC4 (mới) khẳng định tính bất biến. **Hiện trạng (`ceb0ee1`)**: entity `Scan` (`src/ProductionMES.Domain/Entities/Scan.cs`) mới chỉ có `ProductionPlanId` tham chiếu, CHƯA có 6 field snapshot — phần "tra cứu theo tem/trạm/Line/thời gian" (AC2/AC3) và phần snapshot (AC1/AC4) đều chưa làm.
**Bổ sung 19/08/2026 (BA):** thêm AC1 (mở rộng snapshot với `OperatorNames`) + AC5 (giới hạn nghiệp vụ, tránh hiểu nhầm là định danh cá nhân) theo yêu cầu bổ sung của Ban quản lý ("cần biết công đoạn đó có ai thực hiện"). Xác nhận qua code (19/08/2026): entity `Scan` (`src/ProductionMES.Domain/Entities/Scan.cs`) CHƯA có field `OperatorNames` — cần dev bổ sung entity + migration + wiring `ScanService` (`BuildScan`/`ToHistoryItemDto`/`ToDto`) + `ScanHistoryItemDto`/`ScanResultDto`. Đây là bổ sung AC cho story đã ✅ Xong trước đó — không tách story mới, cùng phạm vi snapshot đã có ở US-10.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-19

## Lịch sử triển khai (ghi chú backlog)

**14/08**: AC1/AC4 (snapshot 6 field Customer/Model/Lot/Revision/PlannedQuantity/TaktTimeSeconds vào `Scan`) đã code + migration `AddScanSnapshotFields` + test pass (`9f0e299`). **18/08**: AC2 (tra cứu theo tem, sắp theo thời gian) + AC3 (lọc trạm/Line/khoảng thời gian, kết hợp AND, phân trang theo API-Conventions mục 9) đã code xong — `IScanService.GetHistoryAsync`/`ScanService.GetHistoryAsync` (Application) dùng `IRepository<Scan>.FindAsync` với 1 Expression kết hợp AND toàn bộ filter tùy chọn, sắp `ScannedAtUtc` tăng dần rồi phân trang trong Service (không thêm Dapper — dữ liệu lọc theo tem/trạm/Line/thời gian đã đủ đơn giản để dùng EF Core qua Repository sẵn có, theo đúng "EF Core cho CRUD/query đơn giản"); DTO mới `ScanHistoryQuery`/`ScanHistoryItemDto`/`PagedResult<T>` (`src/ProductionMES.Application/DTOs/Scans/`, `DTOs/Common/PagedResult.cs` — envelope `{items,totalCount,page,pageSize}` đầu tiên trong dự án áp đúng mục 9 API-Conventions, trước đây "chưa implement ở backend"). Endpoint mới `GET api/v1/scans/history` (`ScanHistoryController`, tách riêng khỏi `ScansController` vì khác auth scheme — `[Authorize(Policy=Scan.View)]` mặc định Bearer/cookie theo ADR-004, KHÔNG dùng `StationApiKey` như `ScansController.Create`/ADR-005), query params `tagCode/workStationId/lineId/from/to/page/pageSize` kết hợp AND. Permission mới `Scan.View` (`PermissionResource.Scan=7`, `PermissionPolicies.ScanView`) seed cho `Admin`+`Supervisor`+`Manager` (Manager lần đầu có permission — đúng vai trò "Ban quản lý xem báo cáo" của `UserRole.Manager`), có `EnsureScanViewPermissionGrantsAsync` (`DbSeeder`) phòng gap seed như US-05 Supervisor trước đây. Test mới `ScanServiceHistoryTests` (8 test: tìm theo tem + sắp xếp thời gian, lọc riêng trạm/Line/khoảng thời gian, kết hợp AND, phân trang đúng TotalCount, Page/PageSize không hợp lệ tự chỉnh mặc định, và AC4 test qua chính API tra cứu — snapshot không đổi dù `ProductionPlan` bị sửa sau). Build `dotnet build ProductionMES.sln` sạch (0 lỗi/0 warning), 149/149 test Application pass (`dotnet test`). **Giả định kỹ thuật** (không có trong AC, cần lưu ý nếu BA thấy cần chốt lại): mặc định `pageSize=20`, tự chỉnh về mặc định nếu `page<1` hoặc `pageSize` ngoài [1,200]; thứ tự sắp xếp cho AC3 (không chỉ AC2) cũng dùng tăng dần theo `ScannedAtUtc` để đơn giản hóa (1 code path chung cho cả AC2/AC3); so khớp `TagCode` là so khớp tuyệt đối (không tính case-insensitive dù MySQL collation mặc định có thể case-insensitive ở tầng DB thật). UI tra cứu (web-admin) CHƯA làm — ngoài phạm vi giao việc lần này (task chỉ yêu cầu AC2/AC3 ở tầng API) **19/08 (BA)**: bổ sung AC1/AC5 mới (snapshot `OperatorNames`) — CHƯA implement, chờ dev. **19/08 (dev, implement AC1/AC5 US-10 + AC12 US-21 — snapshot OperatorNames)**: Đã code xong. `Scan` (Domain) thêm property `OperatorNames` (string, default rỗng) — snapshot `ProductionPlan.OperatorNames` tại thời điểm scan giống 6 field snapshot cũ, NHƯNG remarks ghi rõ field này KHÔNG thuộc nhóm bị khóa tuyệt đối (Tổ trưởng vẫn sửa `ProductionPlan.OperatorNames` tự do sau khi có scan, giống Số lượng/Takt time — không đổi gì ở `ProductionPlanService`/validator liên quan tới khóa field). Migration mới `AddScanOperatorNamesSnapshot` (cột `varchar(500) NOT NULL DEFAULT ''`, khớp maxlength `ProductionPlanConfiguration.OperatorNames`) — thêm `ScanConfiguration.Property(s => s.OperatorNames).HasMaxLength(500)`. `ScanService.BuildScan`/`ToHistoryItemDto`/`ToDto` copy `OperatorNames` từ `ProductionPlan` — áp dụng cho cả `CreateAsync` (luồng OK/từ chối tự động) và `CreateNgAsync` (luồng NG chủ động, US-18). `ScanHistoryItemDto`/`ScanResultDto` bổ sung field `OperatorNames`. Test: `ScanServiceTests.CreateAsync_ScanThanhCong_LuuDungSnapshot6FieldTuProductionPlan` (mở rộng assert `OperatorNames`), `ScanServiceNgTests.CreateNgAsync_HopLe_GhiNhanNgKemLyDoVaNguoiXacNhanDayDu` (mở rộng assert `OperatorNames` cho luồng NG), `ScanServiceHistoryTests` (`MakeScan` + test snapshot bất biến mở rộng assert `OperatorNames` qua `GetHistoryAsync`) — không thêm `[Fact]` mới, chỉ mở rộng test hiện có, tổng vẫn 222/222 pass. `web-admin`: `types/scanHistory.ts` bổ sung `operatorNames: string`; `ScanHistoryDrilldownModal.tsx` (US-21 AC12) thêm cột "Người vận hành" ngay sau "Trạm thực hiện" (render `row.operatorNames || '—'`), tăng `scroll.x` 900→1050. Build `dotnet build ProductionMES.sln` sạch 0 lỗi/0 warning; `dotnet test tests/ProductionMES.Application.Tests` 222/222 pass; `web-admin`: `npm run lint` (oxlint) sạch, `npm run build` (tsc -b && vite build) pass. Không có sai lệch so với chỉ dẫn BA.
