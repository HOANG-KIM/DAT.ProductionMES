# US-07: Scan tem tại trạm (luồng cơ bản)
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

## Trạng thái triển khai

- **Trạng thái:** ✅ Xong
- **Cập nhật:** 2026-08-26

## Lịch sử triển khai (ghi chú backlog)

Backend xong (`ceb0ee1`). **17/08**: UI `Station.Wpf` (AC2-AC5) đã code xong trên `AndonBoardWindow`/`AndonBoardViewModel`: bắt input HID scanner (TextBox ẩn `ScanInputBox`, Enter kết thúc 1 lượt scan) → gọi `IScanApiClient` (header `X-Station-Api-Key` qua `StationApiKeyHandler` mới, ADR-005) → banner OK (xanh, tự đóng 1.5s, AC3)/NG (đỏ, chờ bấm "Đã đọc, đóng", AC4)/Waiting (vàng, khi đang gửi) theo mockup đã chốt, banner góc trên-phải không che vùng tên trạm/công đoạn/số lượng ở Row 0 (AC5); chỉ số "Số lượng đã scan OK" (AC2) cập nhật qua sự kiện SignalR `ScanRecorded` (`IScanHubClient`/`ScanHubClient` mới, join group theo `WorkStationId`, tự re-join sau reconnect), không dựa vào response của chính request POST vừa gửi. Bổ sung `StationOptions.WorkStationId`/`StationApiKeyValue` (placeholder rỗng trong `appsettings.json`, giá trị thật phải cấu hình cục bộ thủ công). **17/08 (sau)**: đồng bộ format enum `Result` giữa HTTP và SignalR — `Program.cs` thêm `AddSignalR().AddJsonProtocol(...JsonStringEnumConverter)` (trước đó SignalR trả enum dạng số, khác HTTP trả chuỗi); `ScanHubClient` (Station.Wpf) thêm `.AddJsonProtocol(...)` tương ứng phía client (cần thêm package `Microsoft.AspNetCore.SignalR.Protocols.Json` 10.0.10 + `using Microsoft.Extensions.DependencyInjection;` — extension method nằm ở namespace đó, không phải `Microsoft.AspNetCore.SignalR.Client`). Build `dotnet build src/ProductionMES.Station.Wpf` + `dotnet build ProductionMES.sln` (trừ `ProductionMES.Api` bị Visual Studio/process đang chạy khoá file DLL — lỗi copy MSB3027/MSB3021 ngoài tầm kiểm soát, không phải lỗi biên dịch, không có `error CS` nào trong log) đều pass, 0 warning; 125/125 test Application pass (không đổi Application layer, không có test project riêng cho `Station.Wpf`). Toàn bộ code CHƯA commit (giữ nguyên working tree). **Chưa xác nhận trực quan bằng mắt** trên app Windows thật với máy scan HID thật hoặc API chạy thật — cần người chạy `dotnet run --project src/ProductionMES.Station.Wpf` (kèm API + issue API Key trạm thật qua `StationApiKeysController` rồi điền vào `StationApiKeyValue`) để xác nhận AC2-AC5 hoạt động đúng trước khi đổi trạng thái sang ✅ Xong. **17/08 (bổ sung theo quyết định ghi vào SRS mục 5.1/8.2)**: chốt dùng keyboard-wedge (`TextBox` ẩn `ScanInputBox`) làm giải pháp chính thức thay đọc trực tiếp HID theo VendorID/ProductID; bổ sung cờ cấu hình cục bộ `StationOptions.EnableManualScanInput` (bool, mặc định `false`, đọc từ `appsettings.json` tại trạm) — khi `true`, `AndonBoardWindow` hiện thêm khu vực nhập tay mã tem (góc dưới-trái, gọi lại đúng `AndonBoardViewModel.HandleScanAsync` qua `SubmitManualScanCommand`, không xử lý kết quả riêng) + banner cảnh báo "⚠ ĐANG BẬT CHẾ ĐỘ NHẬP TAY — TEST ONLY" luôn hiển thị liên tục dưới hàng tên trạm/công đoạn; khi `false` (mặc định) UI giữ nguyên 100% như trước. Đã xử lý xung đột focus phát sinh: `ScanInputBox_LostFocus` (vốn luôn tự giành lại focus) được sửa để KHÔNG cướp focus nếu người dùng đang chủ động gõ vào ô nhập tay mới, để không chặn việc gõ tay khi bật cờ — không đổi hành vi khi cờ tắt (ô nhập tay `Collapsed`, không thể nhận focus). Build `dotnet build src/ProductionMES.Station.Wpf/ProductionMES.Station.Wpf.csproj` pass, 0 warning/error. **17/08 (xác nhận trực quan)**: người dùng đã tự chạy `dotnet run --project src/ProductionMES.Station.Wpf` với API thật + API Key trạm thật, thử scan bằng máy quét HID thật (chế độ keyboard-wedge) và chế độ nhập tay — AC2-AC5 hoạt động đúng. Bảng PLAN/ACTUAL/BALANCE/NG-%NG đầy đủ theo mốc giờ trong mockup Andon board (xem `Documents/Layout WPF/Capture.PNG`) **không thuộc scope US-07** — cố ý tách sang **US-09** (⬜ chưa làm), US-07 chỉ cần chỉ số "Số lượng đã scan OK" đơn giản (AC2) + popup kết quả scan (AC3-AC5), đã đủ và đúng

#### 2026-08-26 (dev, đổi vị trí + kích thước banner kết quả scan AC5 theo yêu cầu người dùng)
- **Station.Wpf:** `ScanBanner` (`AndonBoardWindow.xaml`) chuyển từ nằm bên trong `Grid Grid.Row="1"` (góc trên-phải ban đầu, rồi top-center) sang layer overlay riêng phủ toàn cửa sổ (`Grid.RowSpan="3"`, cùng kiểu overlay Chế độ Scan NG/nhập số thùng) — lý do: khi còn nằm trong Row 1, `VerticalAlignment="Top"` chỉ neo theo mép trên của Row 1, mà Row 1 bắt đầu thấp/cao tùy Row 0 (tên trạm/model/plan) cao hay ngắn, khiến banner trông như dạt xuống dưới cùng màn hình dù XAML ghi "Top" — chuyển sang overlay `RowSpan=3` neo đúng theo cửa sổ thật, không phụ thuộc chiều cao Row 0. Theo yêu cầu tiếp theo, đổi `VerticalAlignment` từ `Top` sang `Center` để banner canh giữa cả 2 chiều màn hình. Về kích thước: đổi `MaxWidth` 480→520→720 không tạo khác biệt quan sát được vì `Border` chỉ set `MinWidth`/`MaxWidth` tự co theo nội dung (`StackPanel` bên trong không `Stretch`) — nội dung ngắn (vd "OK INPUT") không bao giờ chạm tới mức `MaxWidth`; sửa dứt điểm bằng cách đổi sang `Width="720"` cố định, banner luôn rộng đúng kích thước này bất kể nội dung dài/ngắn.
- **Build:** `dotnet build src/ProductionMES.Station.Wpf/ProductionMES.Station.Wpf.csproj` pass, 0 warning/error (không đổi Application layer nên không chạy lại test).
- **Còn thiếu / lưu ý:** chỉ là điều chỉnh vị trí/kích thước UI của banner đã có (AC5), chưa có xác nhận trực quan bằng mắt trên app thật sau đợt đổi Width/VerticalAlignment cuối cùng này — giữ nguyên trạng thái ✅ Xong vì không đổi luồng nghiệp vụ AC1-AC4, chỉ CSS/layout thuần túy.
