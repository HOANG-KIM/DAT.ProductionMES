# ADR-006: Điều hướng Station.Wpf — 2 cửa sổ Windows (Main Screen ⇄ Andon Board), Esc một chiều

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
14/08/2026

## Bối cảnh (Context)

`Station.Wpf` hiện là scaffold trống (`MainWindow` mặc định, chưa có Views/ViewModels nào). Trước khi implement UI cho US-05 (Cài đặt kế hoạch), US-05a/US-05b (Chọn kế hoạch), cần chốt kiến trúc điều hướng tổng thể của ứng dụng, vì màn hình có 2 nhóm nhu cầu khác hẳn nhau chạy song song tại cùng 1 trạm:

1. **Andon board** (đã mockup, xem `station_wpf_dashboard_mockup` memory + artifact "Station Andon Board"): bảng PLAN/ACTUAL/BALANCE và popup kết quả scan OK/NG/WAITING — phải hiển thị **liên tục suốt ca** để công nhân đứng máy nhìn thấy, không được để màn hình khác che mất trong thời gian dài.
2. **Cấu hình của Tổ trưởng nâng quyền** (US-05/US-05a/US-05b): Cài đặt kế hoạch sản xuất, Chọn & áp dụng kế hoạch cho từng công đoạn — thao tác không thường xuyên, chỉ Tổ trưởng vào khi cần, có đăng nhập cá nhân (ADR-005).

Mockup 2 màn hình này (artifact "Station Plan Setup", chốt với người dùng 14/08/2026) đề xuất và người dùng xác nhận: 2 cửa sổ Windows **thật sự riêng biệt**, chuyển qua lại bằng phím `Esc` (từ Andon Board) và `Alt+Tab` (điều hướng chuẩn của Windows).

## Quyết định (Decision)

1. **2 `Window` riêng biệt trong cùng 1 process**, không dùng 1 cửa sổ + điều hướng Page nội bộ cho toàn app:
   - `AndonBoardWindow`: `WindowStyle="None"`, `WindowState="Maximized"`, không có thanh tiêu đề/nút đóng thật — mở mặc định khi ứng dụng khởi động, luôn tồn tại suốt vòng đời process.
   - `MainWindow`: cửa sổ bình thường (có title bar), chứa 1 `Frame`/`ContentControl` điều hướng nội bộ giữa các trang: Trang chủ → `PlanSettingsPage` (US-05) ⇄ `PlanSelectionPage` (US-05b). Chỉ hiện khi Tổ trưởng chủ động chuyển sang.
2. **Không tự bắt phím `Alt+Tab`** (không dùng low-level keyboard hook). Vì cả 2 là cửa sổ Windows thật của cùng ứng dụng, `Alt+Tab` của hệ điều hành tự động cycle đúng giữa 2 cửa sổ này — với điều kiện trạm chạy ở chế độ khóa kiosk (ẩn taskbar/Start Menu, không cho truy cập Desktop hoặc ứng dụng khác), việc khóa kiosk thuộc phạm vi triển khai máy trạm (ngoài scope code), không phải logic trong `Station.Wpf`.
3. **Phím `Esc` chỉ bắt cục bộ trong `AndonBoardWindow`** (qua `PreviewKeyDown`), hành vi 1 chiều — không dùng `Esc` để mở lại `AndonBoardWindow` từ `MainWindow`:
   - Nếu đang có popup kết quả scan mở → `Esc` đóng popup trước (đúng hành vi "scan mã CANCEL" đã có ở mockup dashboard), không chuyển cửa sổ.
   - Nếu không có popup nào mở → `Esc` gọi `MainWindow.Activate()`/`Show()`.
4. Điều hướng **giữa `PlanSettingsPage` và `PlanSelectionPage`** (bên trong `MainWindow`) dùng `Frame`/`Page` nội bộ, không mở thêm `Window` mới cho 2 màn này.
5. Trạng thái đăng nhập Supervisor (access token trong bộ nhớ theo ADR-005) đặt trong 1 service singleton (`ISupervisorSessionService`) dùng chung cho cả `MainWindow` lẫn `AndonBoardWindow` trong cùng phiên chạy — đăng xuất xoá token khỏi bộ nhớ và điều hướng `MainWindow` về Trang chủ.

## Lý do (Rationale)

1. **Windows đã cung cấp `Alt+Tab` miễn phí** — không cần tự bắt phím ở tầng ứng dụng, tránh rủi ro của low-level keyboard hook (dễ vỡ khi có ứng dụng khác cùng hook phím, khó debug, hành vi không nhất quán giữa các phiên bản Windows).
2. **Tách cửa sổ vật lý đúng với thực tế vận hành**: Andon board phải luôn có mặt trên màn hình cho công nhân nhìn thấy số liệu — nếu chỉ dùng 1 cửa sổ với Page điều hướng nội bộ, lúc Tổ trưởng cấu hình xong quên bấm "quay lại", Andon board sẽ biến mất khỏi tầm nhìn công nhân trong suốt thời gian đó, ảnh hưởng vận hành dây chuyền. 2 cửa sổ riêng đảm bảo `AndonBoardWindow` luôn tồn tại sẵn ở dưới, chỉ tạm thời bị `MainWindow` che.
3. **`Esc` 1 chiều tránh xung đột nghĩa** với hành vi `Esc` đã dùng để đóng/hủy popup — nếu `Esc` cũng dùng để mở lại Andon board từ Main Screen, dễ gây thao tác nhầm khi Tổ trưởng đang gõ dở form (vd nhấn Esc để hủy 1 trường nhập liệu lại vô tình chuyển cửa sổ).

## Phương án khác đã xem xét (Alternatives Considered)

**1 cửa sổ duy nhất, điều hướng toàn bộ (kể cả Andon board) bằng Page nội bộ**
- *Ưu điểm*: đơn giản hơn về quản lý vòng đời cửa sổ, không cần lo đồng bộ 2 `Window` cùng lúc.
- *Vì sao không chọn*: vi phạm yêu cầu "Andon board phải luôn hiển thị cho công nhân xem" — chuyển Page nghĩa là Andon board bị thay thế hoàn toàn khỏi màn hình, không còn cửa sổ nào giữ nó hiển thị nền.

**Tự bắt `Alt+Tab` bằng low-level keyboard hook để custom hành vi chuyển cửa sổ**
- *Ưu điểm*: kiểm soát hoàn toàn hành vi, không phụ thuộc cấu hình kiosk của máy trạm.
- *Vì sao không chọn*: `Alt+Tab` là phím tắt cấp hệ điều hành, hook ở tầng ứng dụng dễ xung đột với các hook khác, tăng rủi ro không đáng có so với lợi ích (Windows đã làm đúng việc này).

## Hệ quả (Consequences)

**Tích cực**
- Andon board không bao giờ "biến mất" khỏi máy trạm do quên thao tác của Tổ trưởng.
- Tận dụng `Alt+Tab` chuẩn của Windows, không tốn code/rủi ro hook phím.
- `Esc` nhất quán 1 nghĩa duy nhất theo ngữ cảnh (đóng popup trước, chuyển cửa sổ sau).

**Tiêu cực / Rủi ro cần lưu ý**
- Cần cấu hình khóa kiosk ở máy trạm (ẩn taskbar, chặn Desktop) để `Alt+Tab` chỉ cycle đúng 2 cửa sổ này — thuộc phạm vi triển khai hạ tầng máy trạm, cần lưu ý khi viết hướng dẫn cài đặt, không phải lỗi code nếu máy trạm chưa khóa kiosk.
- Quản lý vòng đời 2 `Window` cùng lúc trong `App.xaml.cs`/DI container phức tạp hơn 1 cửa sổ đơn — cần đảm bảo đóng đúng cả 2 khi thoát ứng dụng, tránh rò rỉ tiến trình treo.
- `ISupervisorSessionService` là state dùng chung giữa 2 `Window` — cần cẩn thận thread-safety nếu sau này có thao tác bất đồng bộ đọc/ghi token từ nhiều nơi.

## Ghi chú triển khai

- Gợi ý cấu trúc file: `Views/AndonBoardWindow.xaml`, `Views/MainWindow.xaml` (thay cho `MainWindow.xaml` mặc định hiện tại), `Views/Pages/HomePage.xaml`, `Views/Pages/PlanSettingsPage.xaml`, `Views/Pages/PlanSelectionPage.xaml`, `Services/ISupervisorSessionService.cs`.
- `App.xaml.cs` chịu trách nhiệm khởi tạo DI container (chưa có — cần thêm `Microsoft.Extensions.Hosting`), tạo cả 2 `Window` khi `OnStartup`, hiển thị `AndonBoardWindow` mặc định.
- Tham chiếu mockup: artifact "Station Plan Setup" và "Station Andon Board" (xem memory `station-wpf-dashboard-mockup`, `production-plan-lifecycle-gap`) — dùng làm căn cứ layout khi code Views, không suy đoán lại từ đầu.
