# US-27: Xác nhận Tổ trưởng trước khi lưu lịch sử scan bị từ chối

**Là** Tổ trưởng/Supervisor
**Tôi muốn** phải đăng nhập xác nhận trước khi 1 lượt scan bị hệ thống tự động từ chối (trùng tem/chưa qua công đoạn trước/đang khóa rework...) được lưu vào lịch sử
**Để** tránh lịch sử scan bị nhiễu bởi các lần bấm/quét nhầm ngẫu nhiên của Operator, chỉ giữ lại các tình huống lỗi thực sự cần theo dõi

**Bối cảnh phát sinh:** đảo ngược 1 phần quy tắc nghiệp vụ nền tảng đã chốt từ đầu dự án ("mọi lượt scan kể cả bị từ chối đều tự động lưu lịch sử", SRS FR-10/mục 6 quy tắc 6) — người giao việc yêu cầu thay đổi ngày 25/08/2026 sau khi trao đổi làm rõ qua nhiều vòng với agent `ba` (đã hỏi lại qua AskUserQuestion, chốt rõ từng điểm mở). Xem "Ghi chú (quyết định)" cuối file để biết đầy đủ các phương án đã cân nhắc và lý do chọn.

## Acceptance Criteria

### AC1 — Scan OK không đổi hành vi
- Given công nhân scan 1 tem hợp lệ (qua đủ các bước kiểm tra hiện có, FR-08)
- When hệ thống ghi nhận kết quả OK
- Then lưu ngay lập tức như hiện tại, không yêu cầu xác nhận gì thêm

### AC2 — Scan NG chủ động (US-18, Chế độ Scan NG) không đổi hành vi
- Given công nhân chủ động dùng Chế độ Scan NG (kích hoạt bằng nút "NG"/mã vạch "NG" ở Andon board, đã đăng nhập Tổ trưởng theo đúng US-18 AC1/AC2/AC2a hiện có)
- When xác nhận NG (bấm "Xác nhận NG" sau khi nhập lý do)
- Then lưu ngay như luồng US-18 hiện có — **US-18 giữ nguyên 100%, không đổi bất kỳ điểm nào**, kể cả bước đăng nhập chặn đầu khi kích hoạt chế độ. Cơ chế Lưu/Thoát của US-27 KHÔNG áp dụng cho luồng này.

### AC3 — Scan bị hệ thống tự động từ chối → hiển thị banner Lưu/Thoát, CHƯA lưu gì
- Given công nhân scan 1 tem bị hệ thống tự động từ chối — bao gồm (không giới hạn): trùng tem (`DuplicateTag`), chưa qua công đoạn liền trước (`PreviousStageNotPassed`), đang chờ mở khóa rework (`WaitingReworkUnlock`, US-19), tem trùng tại công đoạn "Đóng thùng" (US-25 — xem AC12 thay thế US-25 AC8), và bất kỳ giá trị `ScanResult` từ chối tự động nào phát sinh sau này
- When hệ thống trả kết quả từ chối
- Then hiển thị banner lỗi (mã tem + lý do từ chối) kèm 2 nút **"Lưu"** và **"Thoát"** — **KHÔNG có bản ghi `Scan` nào được lưu vào lịch sử tại thời điểm này** (khác hẳn hành vi cũ: trước đây bản ghi reject được lưu ngay lúc scan)

### AC4 — Bấm Thoát (hoặc Esc)
- Given banner lỗi (AC3) đang hiển thị
- When người vận hành bấm "Thoát" hoặc nhấn phím Esc
- Then đóng banner ngay, không lưu bất kỳ bản ghi nào (kể cả bản ghi audit), quay về trạng thái chờ scan tiếp theo bình thường; nếu scan lại đúng tem đó ngay sau, hệ thống kiểm tra lại từ đầu theo FR-08/FR-19 hiện có và có thể trả về đúng kết quả từ chối tương tự (không bị coi là "đã từng lưu" vì chưa hề có bản ghi nào được tạo)

### AC5 — Bấm Lưu → yêu cầu đăng nhập Tổ trưởng
- Given banner lỗi (AC3) đang hiển thị
- When người vận hành bấm "Lưu"
- Then hiển thị popup đăng nhập Tổ trưởng (re-auth mỗi lần — không dùng lại phiên đăng nhập dùng chung của các chức năng khác, cùng nguyên tắc đã áp dụng cho US-18/US-19, vì danh tính đăng nhập ở đây sẽ được lưu làm "người xác nhận" trong lịch sử)

### AC6 — Đăng nhập thành công, đủ quyền → lưu thành công
- Given popup đăng nhập (AC5) đang hiển thị
- When tài khoản Supervisor/Admin/Manager đăng nhập đúng và có quyền `Scan.ConfirmReject`
- Then hệ thống lưu 1 bản ghi lịch sử scan với đúng: mã tem, công đoạn, trạm, Line, kế hoạch, **thời gian gốc lúc scan** (không phải lúc xác nhận), kết quả từ chối + lý do đã hiển thị ở AC3, người xác nhận (Id + Username từ token đăng nhập); hiển thị popup "Lưu thành công", **tự động đóng sau 1.5 giây** (đồng bộ thời gian banner OK hiện có), quay về trạng thái chờ scan tiếp theo

### AC7 — Đăng nhập sai hoặc thiếu quyền
- Given popup đăng nhập (AC5) đang hiển thị
- When đăng nhập sai tài khoản/mật khẩu, HOẶC đăng nhập đúng nhưng tài khoản không có quyền `Scan.ConfirmReject`
- Then hiển thị lỗi tương ứng ngay tại popup đăng nhập, KHÔNG lưu gì, cho phép thử đăng nhập lại; banner lỗi gốc (AC3) vẫn còn nguyên phía sau, chưa đóng

### AC8 — Hủy popup đăng nhập
- Given popup đăng nhập (AC5) đang hiển thị
- When người dùng bấm "Hủy" hoặc nhấn Esc
- Then đóng popup đăng nhập, quay lại banner lỗi gốc (AC3) với đầy đủ 2 nút Lưu/Thoát như ban đầu — chưa lưu gì, người vận hành có thể bấm "Lưu" thử lại hoặc "Thoát"

### AC9 — Lỗi mạng/hệ thống khi gửi xác nhận (sau khi đăng nhập thành công)
- Given đã đăng nhập thành công (AC6) nhưng request lưu bị lỗi mạng/API
- When lỗi xảy ra
- Then giữ nguyên banner lỗi gốc, hiển thị thông báo lỗi, cho phép thử lại — không tự động coi như "Thoát" ngoài ý muốn

### AC10 — Cơ chế áp dụng chung cho mọi loại lỗi tự động, kể cả tương lai
- Given hệ thống trả về 1 `ScanResult` từ chối mới (khác `Ok` và khác `Ng`) chưa từng tồn tại tại thời điểm viết AC này
- When lượt scan đó bị từ chối
- Then tự động áp dụng đúng luồng Lưu/Thoát (AC3-AC9) này mà không cần thêm logic đặc thù riêng cho từng loại kết quả — quy tắc client duy nhất: `Result == Ok` → lưu ngay; `Result == Ng` → luồng US-18; còn lại → banner Lưu/Thoát US-27

### AC11 — Không tính vào %NG chất lượng
- Given các bản ghi được lưu qua cơ chế Lưu/Thoát này (DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock...)
- When tính %NG chất lượng ở Andon board
- Then KHÔNG tính vào tử số/mẫu số %NG (giữ nguyên quy tắc hiện có từ US-18 — chỉ `Result=Ng` mới tính)

### AC12 — Tem trùng tại công đoạn "Đóng thùng" (US-25 AC8) — ÁP DỤNG ĐỒNG NHẤT, THAY THẾ AC8
- Given tem trùng tại công đoạn có `Stage` cấu hình là "Đóng thùng" (`IsPackingStage = true`)
- When lượt scan bị từ chối do trùng tem
- Then áp dụng đúng luồng chung AC3-AC9 ở trên — **thay thế hẳn** cơ chế audit riêng của US-25 AC8 (entity `PackingDuplicateScanConfirmation`, vốn giả định bản ghi `Scan` reject đã tồn tại sẵn để tham chiếu `ScanId`). Bấm "Thoát" tại đây = không lưu gì cả (kể cả audit), khác hành vi AC8 cũ (luôn lưu Scan, chỉ audit là tùy chọn) — **quyết định đảo ngược 20/08/2026 lần nữa, đã xác nhận trực tiếp với người giao việc 25/08/2026**. `ConfirmedByUserId`/`ConfirmedByUserName` đã có sẵn trên entity `Scan` (thêm bởi US-18) đủ trả lời "ai xác nhận" — không cần bảng audit `PackingDuplicateScanConfirmation` riêng nữa.

**Nguồn FR:** phát sinh từ rà soát nghiệp vụ 25/08/2026 (agent `ba`, qua nhiều vòng làm rõ với người giao việc) — mở rộng/đảo ngược 1 phần FR-08/FR-10/FR-18/FR-19/FR-25, mục 6 quy tắc 6 và quy tắc 16. Đã đồng bộ vào SRS (xem FR-27 mới, mục 6 quy tắc 6/16 sửa lại, mục 7 AC-31 sửa lại, mục 8.1).
**Phụ thuộc:** US-07/US-08 (luồng scan cơ bản + 2 bước kiểm tra, nguồn phát sinh các `ScanResult` bị từ chối), US-18 (mẫu hình re-auth Tổ trưởng + Controller Bearer riêng, tái sử dụng cho endpoint xác nhận mới), US-19 (`WaitingReworkUnlock` là 1 trong các Result áp dụng), US-22 (phân quyền động — permission mới `Scan.ConfirmReject`), **supersede US-25 AC8** (xem AC12).
**Cờ cảnh báo mục 8.2:** Có — US-20 (Báo cáo tỷ lệ lỗi & nguyên nhân, hiện ⬜ chưa làm) khi triển khai sau này sẽ chỉ thấy được các lượt lỗi **đã được Tổ trưởng chủ động xác nhận lưu**, không phải toàn bộ lượt lỗi thực tế phát sinh tại trạm (lượt bị Thoát không để lại dấu vết nào) — người giao việc đã xác nhận chấp nhận hệ quả này, nhưng cần lưu ý ghi chú rõ trong UI báo cáo khi triển khai US-20 để tránh hiểu nhầm số liệu là đầy đủ.

**Ghi chú (25/08/2026, quyết định — do agent `ba` phân tích qua 2 vòng làm rõ với người giao việc bằng AskUserQuestion):**
- **Phạm vi ban đầu bị hiểu nhầm**: yêu cầu gốc của người giao việc dễ bị nhầm là áp dụng cho US-18 (Chế độ Scan NG, nút đỏ ở Andon board) — đã xác nhận lại trực tiếp: US-18 **hoàn toàn ngoài phạm vi**, giữ nguyên 100%. Đối tượng thực sự là banner lỗi của luồng scan bình thường (US-07/US-08) khi bị hệ thống tự động từ chối.
- **Quyết định cốt lõi (đảo ngược quy tắc nền tảng)**: giữa 2 phương án — (a) đổi hẳn, không tự động lưu nữa, chờ Tổ trưởng xác nhận mới lưu; (b) giữ nguyên auto-save + thêm audit riêng kiểu US-25 AC8 cũ — người giao việc chọn **(a)**, chấp nhận hệ quả mất audit trail cho các lượt bị Thoát (xem "Cờ cảnh báo" ở trên).
- **US-25 AC8**: giữa 2 phương án — áp dụng đồng nhất US-27 (thay thế AC8) hay giữ nguyên AC8/loại trừ đóng thùng — người giao việc chọn **áp dụng đồng nhất, thay thế AC8** (xem AC12).
- **Thời gian tự tắt popup "Lưu thành công"**: giữa 2 phương án (tự tắt 1-2s giống banner OK / cần bấm "Đã đọc, đóng" thủ công giống banner NG) — người giao việc chọn **tự tắt 1.5s giống banner OK** (AC6).
- **Kỹ thuật chưa triển khai (để `dev` tự quyết theo convention hiện có, không phải AC bắt buộc)**:
  - **Backend**: tách `POST api/v1/scans` (`ScansController.Create`, scheme `StationApiKey`, ADR-005) thành 2 bước — Bước 1 (kiểm tra, không đổi scheme): nếu `Result != Ok` thì build DTO trả về nhưng **không gọi `AddAsync`/`SaveChangesAsync`**. Bước 2 (xác nhận lưu, endpoint mới vd `POST api/v1/scans/reject-confirmations`, Bearer mặc định + `[Authorize(Policy=...)]`, permission mới `Scan.ConfirmReject` — `PermissionAction` tiếp theo sau `ConfirmDuplicate=11`, seed Supervisor/Admin/Manager) — client gửi lại **nguyên vẹn** DTO đã nhận ở Bước 1 (Result/RejectionReason/snapshot 7 field/ScannedAtUtc...), server chỉ validate `Result != Ok` rồi lưu kèm `ConfirmedByUserId/Name` từ claim JWT — **KHÔNG chạy lại 3 bước kiểm tra FR-08/US-19** ở bước xác nhận (bản ghi phải phản ánh đúng thời điểm scan gốc, không phải thời điểm Tổ trưởng đăng nhập xong — nhất quán nguyên tắc snapshot đã có ở FR-10/US-10). Mẫu hình tái sử dụng đúng cách `ScanNgController`/`CreateNgAsync` đã làm cho US-18.
  - **Xác nhận an toàn của 3 rule kiểm tra hiện có**: đã rà soát code (`ScanService.CreateAsync`, `ReworkLockCalculator.IsLocked`) — cả `DuplicateTag`, `PreviousStageNotPassed`, `WaitingReworkUnlock` chỉ xét bản ghi `Result=Ok` (hoặc `Ng` mới nhất cho rework lock), **không** phụ thuộc bản ghi reject nào — việc không lưu bản ghi reject ngay lúc scan **không phá vỡ** logic hiện có. Rủi ro duy nhất: nếu tương lai có `ScanResult` mới mà logic của nó tự suy luận dựa trên *lịch sử các lần bị từ chối trước đó* thì cần dev rà lại (chưa có case nào như vậy hiện tại).
  - **US-25**: xóa/không dùng nữa entity `PackingDuplicateScanConfirmation` (nếu đã migration) khi triển khai AC12, thay bằng dùng `Scan.ConfirmedByUserId/ConfirmedByUserName` (đã có sẵn từ US-18) cho công đoạn Đóng thùng luôn.
  - **WPF**: `AndonBoardViewModel.ShowErrorBanner` đổi từ `RequiresAcknowledgement` (1 nút "Đã đọc, đóng") sang state mới 2 nút Lưu/Thoát + Esc; bấm "Lưu" mở `LoginDialog` (permission `Scan.ConfirmReject`) tương tự mẫu `AuthenticateForNgMode`/`ConfirmPackingDuplicateAsync` đã có.

---

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-26

## Lịch sử triển khai (ghi chú backlog)

#### 2026-08-26 (dev, fix chặn scan chồng lên banner Lưu/Thoát + xác nhận test UI thật OK)
- **Station.Wpf:** `AndonBoardViewModel.HandleScanAsync` thiếu guard đầu hàm cho `RequiresAcknowledgement`/`RequiresRejectDecision` — hệ quả: khi banner Lưu/Thoát (AC3) hoặc banner "NG đã ghi nhận" (US-18) đang hiện, quét tiếp 1 tem OK vẫn lọt qua và được lưu bình thường, bỏ qua yêu cầu Tổ trưởng phải xử lý banner trước (phát hiện qua test thực tế bằng UI). Thêm điều kiện chặn MỌI lượt scan tiếp theo (kể cả mã vạch "NG") khi 1 trong 2 cờ này còn `true`, cho tới khi Lưu/Thoát/Đã đọc-đóng (hoặc Esc).
- **Test:** người giao việc đã tự test UI thật (banner Lưu/Thoát, popup đăng nhập, Esc, chặn scan chồng) — xác nhận OK.
- **Build:** `dotnet build ProductionMES.sln` không phát sinh lỗi biên dịch (chỉ MSB3027 do app đang chạy khi build, không liên quan code).
- **Lưu ý deploy:** migration `DropPackingDuplicateScanConfirmation` cần chạy `dotnet ef database update` khi lên môi trường có MySQL thật (chưa áp dụng trong môi trường build/dev này).

#### 2026-08-26 (dev, implement đầy đủ AC1-AC12 — backend hoàn chỉnh, Station.Wpf UI chưa test bằng UI thật)
- **Domain:** thêm `PermissionAction.ConfirmReject = 12` (kế tiếp `ConfirmDuplicate = 11`, giữ nguyên không renumber); cập nhật doc `PermissionResource.Scan`/`PermissionResource.PackingBox` ghi chú supersede. Xóa hẳn entity `PackingDuplicateScanConfirmation` (US-25 AC8 — thay bằng `Scan.ConfirmedByUserId/ConfirmedByUserName` đã có từ US-18, đúng AC12).
- **Application:**
  - `ScanService.CreateAsync`: 3 nhánh từ chối tự động (`WaitingReworkUnlock`/`DuplicateTag`/`PreviousStageNotPassed`, kể cả tại "Đóng thùng") đổi từ `SaveAndReturnAsync` sang `ToDto(scan)` — KHÔNG còn `AddAsync`/`SaveChangesAsync` (AC3/AC10/AC12). Nhánh `Ok` và `CreateNgAsync` (US-18) giữ nguyên 100% (AC1/AC2).
  - Thêm `IScanService.ConfirmRejectedScanAsync`/`ScanService.ConfirmRejectedScanAsync` — validate `Result != Ok/Ng` + người xác nhận, dựng lại `Scan` từ snapshot client gửi (KHÔNG chạy lại 3 rule FR-08/US-19, giữ `ScannedAtUtc` gốc — AC6), lưu kèm `ConfirmedByUserId/Name` từ claim.
  - DTO mới `ConfirmRejectedScanRequest` + validator `ConfirmRejectedScanRequestValidator` (Result phải khác Ok/Ng).
  - Xóa `IPackingBoxService.ConfirmDuplicateAsync`/`PackingBoxService.ConfirmDuplicateAsync` (US-25 AC8) — supersede bởi AC12. Xóa DTO `ConfirmPackingDuplicateRequest`/`PackingDuplicateConfirmationDto` + validator tương ứng.
- **Infrastructure:**
  - Xóa `PackingDuplicateScanConfirmationConfiguration`, bỏ `DbSet<PackingDuplicateScanConfirmation>` khỏi `ApplicationDbContext`.
  - Migration mới `20260826015512_DropPackingDuplicateScanConfirmation` (DropTable) — `dotnet ef migrations has-pending-model-changes` xác nhận không còn lệch model sau migration.
  - `DbSeeder`: thêm `(Scan, ConfirmReject)` vào catalog (tự động seed cho Supervisor/Admin/Manager qua rule sẵn có theo Resource=Scan) + `EnsureScanConfirmRejectPermissionGrantsAsync` (lưới an toàn, idempotent). Bỏ `(PackingBox, ConfirmDuplicate)` khỏi catalog + bỏ `EnsurePackingBoxPermissionGrantsAsync` khỏi phạm vi action đó (method vẫn generic, tự thu hẹp theo catalog).
- **Api:**
  - `ScansController.Create`: không đổi code (đã đúng — chỉ gọi `_scanService.CreateAsync`), chỉ cập nhật doc comment phản ánh hành vi mới.
  - Controller mới `ScanRejectConfirmationsController` (`POST api/v1/scans/reject-confirmations`, Bearer mặc định + policy `Scan.ConfirmReject`) — cùng pattern `ScanNgController`.
  - Xóa `PackingDuplicateConfirmationsController` (US-25 AC8).
  - `PermissionPolicies`: thêm `ScanConfirmReject`, xóa `PackingBoxConfirmDuplicate`. `Program.cs`: thêm/xóa `AddPermissionPolicy` tương ứng.
- **Station.Wpf:**
  - `IScanApiClient`/`ScanApiClient`: thêm `ConfirmRejectedScanAsync` (gửi lại nguyên vẹn `ScanResultDto` đã nhận, Bearer token riêng cho lượt xác nhận — cùng pattern `CreateNgAsync`).
  - `ScanBannerKind`: thêm `Saved` (banner "Lưu thành công", màu xanh, tự đóng 1.5s).
  - `AndonBoardViewModel`: thêm `RequiresRejectDecision` (2 nút Lưu/Thoát) tách biệt với `RequiresAcknowledgement` (banner "NG đã ghi nhận" US-18, giữ nguyên); thêm `SaveRejectedScanCommand`/`ExitRejectedScanCommand`; thêm `ShowRejectBanner`/`ShowSavedBanner`; `HandleScanAsync` nhánh `Result != Ok` nay gán `_pendingRejectedScanResult` thay vì đặc cách riêng cho `IsPackingStage && DuplicateTag` (AC12 áp dụng đồng nhất). Xóa `ConfirmPackingDuplicateAsync`/`PackingDuplicateConfirmOutcome`/`PackingBoxConfirmDuplicatePermission`/`_pendingPackingDuplicateTagCode` (US-25 AC8).
  - `AndonBoardWindow.xaml`: banner lỗi nay có 2 nhóm nút — "ĐÃ ĐỌC, ĐÓNG" (RequiresAcknowledgement, US-18 không đổi) và "THOÁT"/"LƯU" (RequiresRejectDecision, US-27 mới).
  - `AndonBoardWindow.xaml.cs`: `Window_PreviewKeyDown` thêm nhánh Esc cho `RequiresRejectDecision` → `ExitRejectedScanCommand` (AC4), kiểm tra TRƯỚC nhánh `RequiresAcknowledgement` cũ.
  - `IPackingBoxApiClient`/`PackingBoxApiClient`: xóa `ConfirmDuplicateAsync` (US-25 AC8). Xóa model `ConfirmPackingDuplicateRequest`/`PackingDuplicateConfirmationDto` (Station.Wpf/Models).
- **Test:** cập nhật `ScanServiceTests`/`ScanServiceReworkLockTests`/`ScanServicePackingTests` (assert KHÔNG còn `AddAsync` cho 3 nhánh từ chối tự động, kể cả tại "Đóng thùng"); thêm `ScanServiceRejectConfirmationTests` mới (AC5/AC6/AC10/AC12 + validate Result Ok/Ng bị từ chối + thiếu người xác nhận); xóa 2 test `ConfirmDuplicateAsync_*` khỏi `PackingBoxServiceTests`. Tổng 355 test, PASS toàn bộ.
- **Build:** `dotnet build ProductionMES.sln` — 0 Warning, 0 Error (toàn bộ 7 project kể cả Station.Wpf/WPF XAML).
- **Còn thiếu / lý do giữ 🟡:** Station.Wpf (`AndonBoardViewModel`/`AndonBoardWindow.xaml`) mới chỉ build thành công qua compiler — CHƯA chạy thử UI thật (cần máy Windows có màn hình trạm + API thật đang chạy) để xác nhận banner Lưu/Thoát, popup đăng nhập, Esc, và popup "Lưu thành công" tự đóng 1.5s hoạt động đúng như mockup. Migration `DropPackingDuplicateScanConfirmation` cũng chưa áp dụng lên DB thật (không có MySQL server trong môi trường build này) — cần chạy `dotnet ef database update` khi deploy.
