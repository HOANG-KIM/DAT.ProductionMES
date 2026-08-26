# US-25: Quét tem đóng thùng — đếm số lượng, tự động in tem thùng
**Là** Công nhân vận hành (Operator) tại trạm Đóng thùng, có sự hỗ trợ của Tổ trưởng khi cần xác nhận
**Tôi muốn** quét lần lượt tem sản phẩm để hệ thống tự đếm đủ số lượng theo quy cách của Model, tự in tem dán thùng khi đủ và tự chuyển sang thùng kế tiếp
**Để** đóng gói đúng số lượng/thùng theo quy cách, có tem nhận diện thùng hàng, và không phải đếm/tính tay số thùng

**Acceptance Criteria**
- **AC1 — Công đoạn "Đóng thùng" là 1 Stage bình thường trong trình tự**
  - Given "Đóng thùng" đã được khai báo là 1 Stage trong danh mục Công đoạn (US-02) và được cấu hình trong trình tự công đoạn của Line (US-03, `LineStageSequence`)
  - When trạm gắn với công đoạn "Đóng thùng" hoạt động
  - Then mọi lượt scan tại đây tuân thủ đúng FR-08 như bất kỳ Stage nào khác (không có luồng/API riêng ngoài quy tắc chung)
- **AC2 — Scan tem hợp lệ — cộng dồn vào thùng hiện tại**
  - Given tem đã qua đúng công đoạn liền trước (theo trình tự của Line) và chưa từng được quét OK tại "Đóng thùng"
  - When Operator quét tem sản phẩm
  - Then hệ thống ghi nhận scan OK, tăng số lượng đã quét của thùng hiện tại thêm 1, cập nhật hiển thị ngay tại trạm
- **AC3 — Chưa qua công đoạn liền trước**
  - Given tem chưa từng được ghi nhận OK ở công đoạn liền trước (theo trình tự của Line) tại bất kỳ trạm nào trong hệ thống
  - When Operator quét tem tại "Đóng thùng"
  - Then hệ thống từ chối, nêu rõ tên công đoạn còn thiếu, lưu lịch sử lượt scan bị từ chối, không cộng vào số lượng thùng hiện tại
- **AC4 — Đủ số lượng theo quy cách — tự động in tem thùng và chuyển thùng kế tiếp**
  - Given số lượng đã quét trong thùng hiện tại vừa đạt đúng Quy cách đóng gói của Model đang chạy (US-24)
  - When lượt scan đạt đủ số lượng được ghi nhận
  - Then hệ thống tự động in 1 tem dán thùng gồm tối thiểu: Model, Tên sản phẩm, Nhà sản xuất, số lượng sản phẩm/thùng, Khối lượng, Số thùng (BoxNo), ngày giờ đóng thùng, tên trạm/Line thực hiện — theo đúng mẫu tem (template) đã cấu hình cho Model đó; đồng thời bộ đếm reset về 0 và số thùng tự động tăng thêm 1 cho thùng kế tiếp, sẵn sàng nhận tem mới
- **AC5 — Nhập số thùng bắt đầu — lần đầu đóng thùng của 1 kế hoạch**
  - Given kế hoạch sản xuất hiện tại chưa từng có thùng nào được đóng qua công đoạn "Đóng thùng" này
  - When Operator/Tổ trưởng bắt đầu phiên đóng thùng đầu tiên cho kế hoạch đó
  - Then hệ thống bắt buộc nhập số thùng bắt đầu (BoxNo khởi điểm) trước khi cho quét tem đầu tiên, để nối tiếp đúng số nếu kế hoạch đã có thùng đóng trước đó bằng cách khác
- **AC6 — Tự động tăng số thùng, tự nhớ trạng thái đang dở**
  - Given đã đóng xong ít nhất 1 thùng cho kế hoạch hiện tại (đã qua AC5)
  - When Operator tiếp tục quét tem cho thùng kế tiếp, kể cả sau khi tắt/mở lại ứng dụng hoặc rời màn hình rồi quay lại
  - Then hệ thống tự hiển thị đúng số thùng hiện tại và số lượng đã quét dở của thùng đó, không yêu cầu nhập lại số thùng
- **AC7 — Sửa số thùng hiện tại cần Supervisor xác nhận**
  - Given đang đóng thùng dở (đã có BoxNo hiện tại), phát hiện cần sửa lại số thùng (nhập sai/cần đóng bù)
  - When Operator/Tổ trưởng chọn sửa số thùng hiện tại
  - Then hệ thống yêu cầu đăng nhập nâng quyền Supervisor (tái sử dụng cơ chế của US-18) trước khi cho phép đổi giá trị; đổi thành công thì số thùng mới có hiệu lực ngay cho lượt quét tiếp theo
- **AC8 — Tem trùng — yêu cầu Supervisor xác nhận đã biết tình huống (KHÔNG cộng số lượng)** *(chốt lại 20/08/2026, đảo ngược giả định ban đầu — **SUPERSEDED 25/08/2026 bởi US-27**, xem ghi chú ngay dưới)*
  - **⚠️ Ghi chú (25/08/2026): AC8 dưới đây đã bị THAY THẾ bởi `US-27` (`Documents/backlog/US-27-xac-nhan-to-truong-truoc-khi-luu-scan-bi-tu-choi/README.md`)** — quyết định mới: bản ghi Scan cho lượt tem trùng tại "Đóng thùng" KHÔNG còn tự động lưu ngay lúc từ chối như mô tả ở AC8 gốc; chỉ lưu khi Supervisor đăng nhập bấm "Lưu" (áp dụng đúng cơ chế chung của US-27 AC3-AC9/AC12), bấm "Thoát" thì không lưu gì kể cả audit — khác hẳn hành vi "luôn lưu Scan, chỉ audit tùy chọn" mô tả bên dưới. Giữ nguyên nội dung AC8 gốc để tham chiếu lịch sử, KHÔNG dùng làm đặc tả hiện hành khi triển khai — xem US-27 AC12 để biết đặc tả mới.
  - Given tem đã từng được quét OK tại công đoạn "Đóng thùng" trước đó (trùng theo đúng phạm vi FR-08 — Mã tem + Công đoạn)
  - When Operator quét lại đúng tem đó
  - Then hệ thống từ chối ngay lập tức theo đúng FR-08 (không ghi đè), lưu lịch sử lượt bị từ chối, và mở popup đăng nhập nâng quyền Supervisor (tái sử dụng đúng cơ chế popup đăng nhập của US-18) để **xác nhận đã biết tình huống** — xác nhận thành công CHỈ lưu vết audit (ai đã xử lý), **KHÔNG cộng thêm số lượng vào thùng hiện tại, KHÔNG tạo thêm bản ghi OK cho tem đó**. Đây KHÔNG phải ngoại lệ ghi đè của FR-08 — hành vi từ chối vẫn giữ nguyên như mọi công đoạn khác (mục 6 quy tắc 16)
  - *(điều chỉnh 24/08/2026, sau phản hồi thực tế — tem trùng ở đây phần lớn do máy quét đọc trùng 2 lần liên tiếp cùng 1 tem chứ không phải tình huống cần xử lý nghiệp vụ):* Operator/Tổ trưởng bấm **Hủy** ở popup đăng nhập vẫn được đóng banner NGAY, không bị ép buộc đăng nhập — chỉ KHÔNG có bản ghi audit "ai đã xác nhận" gắn với lượt đó (bản ghi Scan bị từ chối ở trên vẫn giữ nguyên lịch sử, đúng AC10). Banner CHỈ giữ nguyên (bắt thử lại) khi việc xác nhận thất bại do lỗi hệ thống thật sự (mạng/API) SAU KHI Supervisor đã đăng nhập thành công.
- **AC12 — Đổi Quy cách đóng gói khi đang đóng dở 1 thùng** *(bổ sung 20/08/2026)*
  - Given thùng hiện tại đang đóng dở (đã quét 1 phần, chưa đủ), số lượng mục tiêu của thùng này đã được snapshot đúng Quy cách đóng gói tại thời điểm mở thùng
  - When Admin/Tổ trưởng sửa Quy cách đóng gói của Model đó (US-24/AC2) ngay trong lúc thùng đang đóng dở
  - Then thùng đang dở tiếp tục dùng đúng số lượng mục tiêu đã snapshot (không đổi theo), quy cách mới chỉ áp dụng cho thùng MỞ SAU thời điểm sửa (mục 6 quy tắc 17)
- **AC9 — Theo dõi thời gian thực tại trạm**
  - Given đang trong 1 phiên đóng thùng
  - When có lượt quét OK mới được ghi nhận
  - Then màn hình trạm cập nhật ngay số lượng đã quét trong thùng hiện tại / quy cách cần đủ, không cần thao tác làm mới thủ công
- **AC10 — Lưu lịch sử đầy đủ, không có scan biến mất**
  - Given bất kỳ lượt quét nào tại công đoạn "Đóng thùng" (OK, chưa qua công đoạn trước, trùng tem bị từ chối, trùng tem đã được Supervisor xác nhận)
  - When lượt quét xảy ra
  - Then hệ thống lưu lại lịch sử đầy đủ, nhất quán với nguyên tắc chung "mọi lượt scan đều lưu lịch sử" của toàn hệ thống (mục 6 quy tắc 6)
- **AC11 — Chưa có cấu hình đóng gói cho Model**
  - Given kế hoạch đang chạy tại "Đóng thùng" có Model chưa được cấu hình quy cách đóng gói (US-24)
  - When Operator cố quét tem đầu tiên
  - Then hệ thống chặn, báo lỗi rõ ràng yêu cầu cấu hình Quy cách đóng gói cho Model trước, không ghi nhận lượt scan là hợp lệ
- **AC13 — Lỗi in không chặn đóng thùng kế tiếp, luôn có In lại thủ công** *(chốt 20/08/2026)*
  - Given tem dán thùng vừa được lệnh in tự động (AC4) nhưng không ra giấy được (lỗi vật lý máy in — kẹt/hết giấy — không phát hiện được ở tầng code, khác lỗi lệnh gọi cứng như thiếu template/Excel/máy in mặc định)
  - When Operator/Tổ trưởng tiếp tục quét tem cho thùng kế tiếp
  - Then hệ thống KHÔNG chặn, cho phép đóng thùng kế tiếp bình thường; luôn có sẵn thao tác "In lại" để chủ động in bù bất cứ lúc nào khi phát hiện tem chưa ra giấy — chỉ chặn/báo lỗi khi CHÍNH lệnh gọi in thất bại (thiếu template/Excel/máy in)
- **AC14 — Trạm KHÔNG phải Đóng thùng không có bất kỳ chức năng nào ở trên** *(làm rõ 20/08/2026, không phải AC mới về mặt nghiệp vụ)*
  - Given 1 trạm được cấu hình cho công đoạn KHÁC "Đóng thùng" (vd Lắp ráp, Thông điện...) — mỗi `WorkStation` gắn cố định đúng 1 cặp (Line, Công đoạn)
  - When Operator vận hành scan tại trạm đó
  - Then hệ thống chạy đúng luồng scan tiêu chuẩn hiện có (US-07/US-08, popup OK/NG/WAITING) — KHÔNG hiển thị bộ đếm số lượng/số thùng, KHÔNG tự động in tem; tem trùng vẫn bị từ chối cứng theo đúng FR-08 mặc định, KHÔNG có bước "Supervisor xác nhận đã biết" như AC8 (đặc thù riêng của Đóng thùng, không áp dụng công đoạn khác)

**Nguồn FR:** FR-25
**Phụ thuộc:** US-24 (cấu hình quy cách — bắt buộc có trước, AC11), US-01/US-02/US-03 (khai báo Stage + trình tự), US-07/US-08 (nền tảng luồng scan + FR-08 kế thừa trực tiếp), US-18 (mẫu tham chiếu bắt buộc cho AC7/AC8 — cơ chế re-auth Supervisor mỗi lần), US-16 (Hàng đợi cục bộ chống mất lượt scan — hiện ⬜ Chưa làm; phụ thuộc gián tiếp vì đếm lũy kế theo thùng nhạy với mất đồng bộ hơn scan độc lập từng lượt, cân nhắc thứ tự triển khai)
**Cờ cảnh báo mục 8.2:** Không còn — cả 3 điểm mở phát sinh từ US-25 đã chốt 20/08/2026 (xem AC8, AC12, AC13).

---

## Trạng thái triển khai

- **Trạng thái:** 🟡 Một phần
- **Cập nhật:** 2026-08-24

## Lịch sử triển khai (ghi chú backlog)

#### 2026-08-24 (dev, 2 sửa nhỏ theo phản hồi thực tế sau khi triển khai — gợi ý số thùng + nới lỏng AC8)

- **AC5 (gợi ý số thùng bắt đầu):** overlay nhập số thùng lần đầu (`AndonBoardViewModel.ApplyPackingState`) giờ prefill sẵn `"1"` thay vì để trống — Operator vẫn sửa được nếu cần nối số theo thùng đã đóng trước đó bằng cách khác (AC5 KHÔNG đổi, vẫn cho nhập số bất kỳ >0, không ép cứng = 1 — đã xác nhận với người giao việc đây là hành vi cố ý, không phải bug).
- **AC8 (nới lỏng yêu cầu đăng nhập khi Hủy):** `AcknowledgeBannerAsync`/`ConfirmPackingDuplicateAsync` đổi từ trả `bool` sang enum `PackingDuplicateConfirmOutcome` (`Confirmed`/`Cancelled`/`Failed`) — Hủy popup đăng nhập (`Cancelled`) giờ cho đóng banner NGAY, không ép buộc đăng nhập như trước; chỉ giữ nguyên banner khi xác nhận thất bại do lỗi hệ thống thật sự SAU KHI đăng nhập thành công (`Failed`). Bản ghi `Scan` (Result=DuplicateTag) không đổi cách lưu — vẫn lưu lịch sử đầy đủ ngay từ lúc FR-08 từ chối, đúng AC10, chỉ không có audit `PackingDuplicateScanConfirmation` khi Operator chọn Hủy.
- **Station.Wpf:** `src/ProductionMES.Station.Wpf/ViewModels/AndonBoardViewModel.cs`.
- **Build:** `dotnet build ProductionMES.sln` sạch 0 Warning/0 Error.
- **Test:** không đổi Application/Domain nên không chạy lại `dotnet test` (chỉ sửa Station.Wpf, không có test tự động cho project này).
- **Còn thiếu:** chưa chạy thử bằng mắt trên máy thật (cùng hạn chế đã ghi ở đợt 2026-08-24 trước — không có công cụ chạy/chụp màn hình Windows app trong phiên này).

Story mới 20/08/2026 — phụ thuộc US-24 (chưa có cấu hình thì chặn scan, AC11). Xem AC đầy đủ ở mục 3.8, nguồn FR-25 (SRS)

#### 2026-08-24 (dev, implement đầy đủ backend + Station.Wpf UI, in tem best-effort)

- **Quyết định thiết kế mới (không có sẵn trong SRS/backlog gốc, ghi lại theo CLAUDE.md):**
  - **Nhận diện công đoạn "Đóng thùng"**: thêm `Stage.IsPackingStage` (bool, mặc định `false`) — Admin CHỦ ĐỘNG đánh dấu đúng 1 Stage khi khai báo danh mục (US-02), KHÔNG suy luận từ `Stage.Name` (free-text, dễ gõ sai/đổi tên). `ScanService.CreateAsync` đọc cờ này để quyết định có chạy thêm bước đặc thù US-25 hay không — AC1 vẫn đúng (không có luồng/API riêng, chỉ thêm hành vi phụ có điều kiện). `Station.Wpf` mirror qua `StationOptions.IsPackingStage` (cấu hình cục bộ theo trạm, cùng nguyên tắc rủi ro chấp nhận được với `LineId`/`StageId` hiện có — server (`ScanResultDto.IsPackingStage`) vẫn là nguồn xác thực cuối cho hành vi đếm/chống trùng).
  - **Permission mới**: `PermissionResource.PackingBox = 10`, `PermissionAction.ConfirmDuplicate = 11` — 2 policy `PackingBox.Update` (AC7, sửa số thùng) và `PackingBox.ConfirmDuplicate` (AC8, xác nhận tem trùng), seed Admin + Supervisor (KHÔNG Manager, cùng phạm vi `Scan.ReworkUnlock`). Đọc trạng thái đóng thùng (GetState)/nhập số thùng bắt đầu (AC5) dùng scheme `StationApiKey` (Operator không cần đăng nhập), TÁCH riêng khỏi 2 action trên (Bearer + permission, tái sử dụng cơ chế re-auth Supervisor của US-18/19) — 3 Controller riêng biệt (`PackingBoxesController`/`PackingBoxUpdatesController`/`PackingDuplicateConfirmationsController`), cùng pattern `ScansController`/`ScanNgController`/`ReworkUnlockController`.
  - **AC8 (chống trùng cần Supervisor xác nhận)**: KHÔNG đổi `ScanService.CreateAsync` cho nhánh DuplicateTag (vẫn từ chối + lưu lịch sử y hệt mọi Stage khác, đúng AC8 "KHÔNG phải ngoại lệ ghi đè") — chỉ thêm entity audit riêng `PackingDuplicateScanConfirmation` (mirror thiết kế `ReworkUnlock`: ghi mỗi lần xác nhận, không phải cờ tĩnh), liên kết `ScanId` tới đúng bản ghi `Scan` (Result=DuplicateTag) gần nhất. `ScanResultDto` bổ sung `IsPackingStage` (gán cho MỌI kết quả, không chỉ Ok) để Station.Wpf biết khi nào cần mở luồng Supervisor xác nhận.
  - **AC12 (snapshot Quy cách đóng gói)**: `PackingBox` snapshot `TargetQuantity`/`Model`/`PartName`/`Manufacturer`/`GrossWeight` tại thời điểm MỞ thùng (không tra cứu động qua `PackingModelConfigId`) — thùng kế tiếp (tự động mở khi AC4) đọc LẠI cấu hình mới nhất qua `IPackingModelConfigService.GetByModelAsync`, không tái dùng snapshot cũ.
  - **In tem (AC4/AC13)**: server merge dữ liệu vào template `.xlsx` đã upload ở US-24 bằng `ClosedXML` (đã có sẵn trong `ProductionMES.Application`, dùng lại đúng thư viện `LotReportExportService`/US-23) — quy ước placeholder tự đặt: token `{{Model}}`/`{{PartName}}`/`{{Manufacturer}}`/`{{PackingQuantity}}`/`{{GrossWeight}}`/`{{BoxNo}}`/`{{PackedAt}}`/`{{LineName}}`/`{{WorkStationName}}` trong cell text được thay bằng giá trị thật (`IPackingLabelGenerator`/`PackingLabelGenerator`). Endpoint `GET api/v1/packing-boxes/{id}/label` trả file `.xlsx` đã merge; `Station.Wpf` tải về rồi in bằng `Process.Start(Verb="print")` (ShellExecute, dựa vào ứng dụng mặc định xử lý .xlsx đã cài tại trạm — KHÔNG tự vẽ trang in). AC4 "tự động in + reset + tăng BoxNo" tách RIÊNG khỏi kết quả in thật: server LUÔN tự động hoàn tất + mở thùng kế tiếp ngay khi đủ số lượng (bất kể in thành công hay không), Station.Wpf tự in nền (fire-and-forget) — đúng tinh thần AC13 "không chặn đóng thùng kế tiếp". Lỗi CHÍNH lệnh gọi in (thiếu template ở server, hoặc `Process.Start` ném `Win32Exception`/`InvalidOperationException` ở trạm — thiếu ứng dụng/máy in mặc định) hiển thị 1 dòng trạng thái nhỏ cạnh bộ đếm thùng, có nút "In lại" luôn sẵn sàng gọi lại đúng endpoint đó cho thùng hoàn tất gần nhất. **Chưa kiểm tra tường minh "có máy in mặc định hay không"** trước khi gọi `Process.Start` (dựa vào exception của chính lệnh gọi) — đủ cho AC13 về mặt hành vi (không chặn, có In lại) nhưng CHƯA test trên máy in vật lý thật.
- **Domain**: `Stage.IsPackingStage`; `PackingBoxStatus` (enum `InProgress`/`Completed`); `PackingBox` (snapshot AC12, `BoxNo`/`TargetQuantity`/`ScannedQuantity`/`Status`/`OpenedAtUtc`/`CompletedAtUtc`, không FK); `PackingDuplicateScanConfirmation` (audit AC8, mirror `ReworkUnlock`); `PermissionResource.PackingBox`/`PermissionAction.ConfirmDuplicate`.
- **Infrastructure**: `PackingBoxConfiguration`/`PackingDuplicateScanConfirmationConfiguration` (index thường, không unique DB), `StageConfiguration` bổ sung `IsPackingStage` (`HasDefaultValue(false)`), 2 `DbSet` mới. Migration `AddPackingBoxAndStagePackingFlag` đã tạo VÀ **áp dụng thành công vào MySQL cục bộ** (`dotnet ef database update`, xác nhận qua `dotnet ef migrations list`). `DbSeeder`: catalog mới `(PackingBox, Update)`/`(PackingBox, ConfirmDuplicate)` + `EnsurePackingBoxPermissionGrantsAsync` (Admin/Supervisor).
- **Application**: `IPackingBoxService`/`PackingBoxService` (`GetStateAsync`/`SetStartingBoxNoAsync`/`UpdateCurrentBoxNoAsync`/`ConfirmDuplicateAsync`/`GenerateLabelAsync`/`EnsureReadyForScanAsync`/`RegisterOkScanAsync`); `IPackingLabelGenerator`/`PackingLabelGenerator` (ClosedXML); DTOs `PackingBoxDto`/`PackingBoxStateDto`/`SetStartingBoxNoRequest`/`UpdateCurrentBoxNoRequest`/`ConfirmPackingDuplicateRequest`/`PackingDuplicateConfirmationDto` + validator tương ứng (FluentValidation). `ScanService.CreateAsync` tích hợp: chặn AC11/AC5 qua `IPackingBoxService.EnsureReadyForScanAsync` TRƯỚC 2 bước kiểm tra FR-08 (KHÔNG lưu Scan cho lỗi cấu hình), cộng dồn/hoàn tất thùng qua `RegisterOkScanAsync` SAU khi lưu Scan Ok; `ScanResultDto` bổ sung `IsPackingStage`/`PackingBoxNo`/`PackingScannedQuantity`/`PackingTargetQuantity`/`PackingBoxCompleted`/`PackingCompletedBoxId`. `Stage`/`CreateStageRequest`/`UpdateStageRequest`/`StageDto`/`StageService` bổ sung `IsPackingStage` (US-02 form, để Admin đánh dấu được).
- **Api**: `PackingBoxesController` (`api/v1/packing-boxes`, StationApiKey — `GET state`/`POST starting-box-no`/`GET {id}/label`), `PackingBoxUpdatesController` (`api/v1/packing-boxes/box-no`, Bearer + `PackingBox.Update`, AC7), `PackingDuplicateConfirmationsController` (`api/v1/packing-boxes/duplicate-confirmations`, Bearer + `PackingBox.ConfirmDuplicate`, AC8). Policy mới trong `PermissionPolicies`/`Program.cs`.
- **web-admin**: `StageFormModal`/`StageListPage` bổ sung checkbox "Là công đoạn Đóng thùng" (AC1, chỉ metadata, không thêm màn hình mới) + cột hiển thị trong bảng danh mục Công đoạn (US-02).
- **Station.Wpf**: `AndonBoardViewModel`/`AndonBoardWindow` (nơi Operator scan, đúng ADR-006) bổ sung: bộ đếm "THÙNG SỐ/ĐÃ ĐÓNG" (AC2/AC9, hiển thị khi `IsPackingStage`), overlay dùng CHUNG cho AC5 (nhập số thùng bắt đầu, bắt buộc, không có nút Hủy) và AC7 (sửa số thùng, cần Supervisor re-auth qua `LoginDialog` — permission `PackingBox.Update`, cùng idiom `AndonBoardViewModel.AuthenticateForNgMode`), nút "In lại" (AC13, luôn sẵn sàng khi kế hoạch đã có ≥1 thùng hoàn tất) + dòng trạng thái lỗi in nhỏ. AC8: `AcknowledgeBannerCommand` đổi thành async — khi banner đang mở là DuplicateTag tại "Đóng thùng", bắt buộc mở `LoginDialog` (permission `PackingBox.ConfirmDuplicate`) xác nhận-đã-biết trước khi cho đóng banner (Hủy/thiếu quyền -> banner GIỮ NGUYÊN). `IPackingBoxApiClient`/`PackingBoxApiClient` (GetState/SetStartingBoxNo/DownloadLabel qua StationApiKey; UpdateCurrentBoxNo/ConfirmDuplicate gắn Bearer tường minh từng request, cùng `IScanApiClient.CreateNgAsync`); `IPackingLabelPrintService`/`PackingLabelPrintService` (tải + `Process.Start(Verb="print")`). `StationOptions.IsPackingStage` (cấu hình cục bộ mới) + `appsettings.json` mẫu.
- **Test**: `PackingBoxServiceTests` (18 test — AC2/AC4/AC5/AC6/AC7/AC8/AC11/AC12/AC13) + `ScanServicePackingTests` (5 test — tích hợp AC1/AC2/AC4/AC5/AC8/AC11/AC14 vào `ScanService.CreateAsync`) trong `ProductionMES.Application.Tests`.
- **Build**: `dotnet build ProductionMES.sln` sạch 0 Warning/0 Error (cả `ProductionMES.Station.Wpf`); `web-admin`: `npm run lint` (oxlint) sạch, `npm run build` (tsc -b && vite build) pass.
- **Test kết quả**: `dotnet test tests/ProductionMES.Application.Tests` 308/308 pass (23 test mới, không có test nào cũ bị fail).
- **Còn thiếu / lý do giữ 🟡**:
  - `Station.Wpf` UI **CHƯA chạy thử bằng mắt trên máy thật** (không có công cụ chạy/chụp màn hình Windows app trong phiên này) — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` với 1 trạm cấu hình `IsPackingStage=true` để xác nhận overlay AC5/AC7, bộ đếm AC2/AC9, luồng AC8 hoạt động đúng trên máy thật.
  - **In tem vật lý (AC4/AC13) chưa test với máy in thật** — pipeline `Process.Start(Verb="print")` mới verify qua build/logic, chưa xác nhận in ra đúng nội dung/khổ giấy tem thùng thật trên phần cứng thực tế. Quy ước placeholder `{{Token}}` trong template cũng là quy ước MỚI (chưa có mẫu tem thật nào dùng thử) — cần người thiết kế 1 file mẫu tem `.xlsx` theo đúng token này để kiểm chứng.
  - AC9 (theo dõi thời gian thực) chỉ cập nhật qua response trực tiếp của lượt scan (không qua SignalR `ScanRecorded` như `ScannedOkCount`) — đủ đúng cho 1 trạm đơn (chỉ Operator tại đúng trạm đó thao tác đóng thùng), nhưng KHÔNG đồng bộ real-time nếu có nhiều nguồn ghi scan cùng lúc cho cùng 1 kế hoạch (kịch bản hiếm, chưa có AC yêu cầu).
