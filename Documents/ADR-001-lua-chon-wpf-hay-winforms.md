# ADR-001: Lựa chọn WPF thay vì WinForms cho ứng dụng trạm làm việc

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
11/08/2026

## Bối cảnh (Context)

Kiến trúc tổng thể của hệ thống DAT.ProductionMES đã được thống nhất: ASP.NET Core Web API trung tâm (3-layer: Controller → Service → Repository, Unit of Work, tuân thủ SOLID), MySQL, EF Core kết hợp Dapper, SignalR cho cập nhật thời gian thực, HID library cho máy scan tem, và Serial (System.IO.Ports) cho giao tiếp Arduino.

Một quyết định kiến trúc còn để ngỏ là nền tảng UI cho **ứng dụng tại từng trạm làm việc** — client chạy tại PC + màn hình (kèm máy scan, một số trạm kèm Arduino) tại mỗi công đoạn. Đây là dạng ứng dụng kiosk vận hành liên tục trong ca sản xuất, không phải công cụ văn phòng đa chức năng, với các đặc điểm nghiệp vụ nổi bật rút ra từ SRS:

- Màn hình phải **tự cập nhật số liệu real-time** ngay khi có lượt scan mới, không qua thao tác làm mới thủ công (FR-09), với yêu cầu phi chức năng độ trễ SignalR ≤ 1 giây (mục 4 — Real-time).
- Giao diện phải thể hiện **nhiều trạng thái động, chuyển đổi liên tục** trong lúc vận hành: thông báo scan OK dạng toast tự ẩn sau 1–2 giây (FR-07), thông báo lỗi khác màu/âm thanh cần xác nhận đã đọc (FR-07), đổi giao diện toàn màn hình khi vào "Chế độ Scan NG" rồi tự quay lại sau 30 giây (FR-18), trạng thái đếm ngược "Đang chờ kết quả kiểm tra Arduino" (FR-12/FR-13), và phân biệt 3 trạng thái đồng bộ bằng màu sắc (FR-17).
- Yêu cầu phi chức năng "Độ tin cậy" (mục 4) đòi hỏi mọi lượt scan luôn ở đúng 1 trong 3 trạng thái rõ ràng trên UI (OK/NG/Chờ đồng bộ) — không có trạng thái "treo" mập mờ.
- Với trạm gộp nhiều công đoạn trên 1 PC (nhiều màn hình), mỗi màn hình phải chạy 1 tiến trình (process) riêng biệt, mỗi công đoạn có máy scan/Arduino riêng (mục 6, quy tắc 7) — ràng buộc này áp dụng như nhau với cả WinForms lẫn WPF, không phải yếu tố phân biệt giữa hai lựa chọn.

Ràng buộc đã biết: đội phát triển đã có **một chút kinh nghiệm với WPF** (không phải hoàn toàn mới), theo xác nhận mới nhất từ khách hàng/PM — làm giảm rủi ro về đường cong học tập vốn là điểm trừ thường được nêu ra khi cân nhắc WPF.

Lưu ý: bản SRS hiện tại (mục 1.2, 2.1, 5.2, mục 4 — Khả năng phục hồi) đang dùng cụm từ "ứng dụng WinForms" để chỉ ứng dụng trạm làm việc; đây là tên gọi tạm thời tại thời điểm viết SRS, chưa phản ánh quyết định kiến trúc chính thức. ADR này là quyết định chính thức, cần được đồng bộ ngược lại vào SRS sau khi được chấp thuận.

## Quyết định (Decision)

Chọn **WPF (Windows Presentation Foundation)** kết hợp mẫu kiến trúc **MVVM (Model-View-ViewModel)** làm nền tảng công nghệ UI cho ứng dụng tại các trạm làm việc, thay cho WinForms.

## Lý do (Rationale)

1. **Data binding hai chiều khớp trực tiếp với luồng cập nhật real-time qua SignalR.** Khi ViewModel nhận sự kiện từ SignalR và cập nhật thuộc tính (số lượng scan, chỉ số âm/dương), UI tự động phản ánh thay đổi mà không cần thao tác làm mới thủ công — đúng yêu cầu FR-09 và ngưỡng độ trễ ≤ 1 giây tại mục 4 (Real-time).

2. **Styles/Triggers/Animation xử lý gọn các trạng thái giao diện động** mà SRS mô tả chi tiết, giảm code thủ công can thiệp trực tiếp vào control (vốn là cách làm điển hình trong WinForms):
   - Toast/banner thông báo scan OK tự biến mất sau 1–2 giây, không che số liệu chính (FR-07).
   - Thông báo lỗi đổi màu/âm thanh khác biệt, yêu cầu xác nhận đã đọc, không tự tắt (FR-07).
   - Đổi giao diện toàn màn hình (nền đỏ, chữ lớn) khi vào "Chế độ Scan NG", tự động quay về mặc định sau 30 giây nếu không có thao tác (FR-18).
   - Trạng thái "Đang chờ kết quả kiểm tra Arduino" kèm đếm ngược timeout, chuyển trạng thái khi có kết quả hoặc hết giờ (FR-12, FR-13).
   - Phân biệt rõ 3 trạng thái đồng bộ bằng màu sắc: Đã xác nhận OK / Đã xác nhận NG / Chờ đồng bộ (FR-17).

3. **Rủi ro đường cong học tập đã giảm đáng kể.** Khuyến nghị ban đầu có lưu ý XAML/MVVM đòi hỏi thời gian làm quen nếu đội chưa có kinh nghiệm; thông tin mới xác nhận đội đã có một chút kinh nghiệm WPF, nên chi phí chuyển đổi không còn là yếu tố cản trở đáng kể.

## Phương án khác đã xem xét (Alternatives Considered)

**WinForms**

- *Ưu điểm*: mô hình lập trình đơn giản, học nhanh, đã ổn định qua nhiều năm, đủ đáp ứng cho các màn hình có tính chất tĩnh, ít trạng thái động.
- *Vì sao không chọn*: WinForms không có cơ chế data binding hai chiều mạnh, việc cập nhật UI theo sự kiện real-time (SignalR) và các trạng thái động dày đặc trong SRS (toast tự ẩn, banner lỗi cần xác nhận, đổi giao diện toàn màn hình theo chế độ, đếm ngược timeout, phân biệt màu theo trạng thái đồng bộ) đòi hỏi xử lý thủ công nhiều hơn ở tầng code-behind (invoke UI thread, tự quản lý timer ẩn/hiện control, tự đổi thuộc tính từng control theo trạng thái). Điều này làm tăng chi phí bảo trì và rủi ro lỗi giao diện khi số lượng trạng thái UI tiếp tục tăng theo yêu cầu nghiệp vụ, so với hướng khai báo (declarative) mà WPF hỗ trợ sẵn.

## Hệ quả (Consequences)

**Tích cực**
- Dễ bảo trì các trạng thái UI động vì logic hiển thị (đổi màu, ẩn/hiện, animation) được khai báo trong XAML (Styles/Triggers) thay vì rải rác trong code-behind.
- Binding tự nhiên với luồng dữ liệu real-time từ SignalR thông qua ViewModel, giảm code cập nhật UI thủ công.
- Tách bạch rõ giao diện (View) và logic hiển thị/trạng thái (ViewModel) giúp việc kiểm thử logic trạng thái (vd: state machine chờ Arduino ở FR-12) dễ hơn, độc lập với UI.

**Tiêu cực / Rủi ro cần lưu ý**
- Vẫn còn đường cong học tập nhất định với XAML và MVVM dù đội đã có một chút kinh nghiệm — cần thời gian làm quen ở giai đoạn đầu dự án, đặc biệt với các thành viên chưa từng dùng MVVM một cách hệ thống (binding, command, converter).
- Cần thống nhất **convention MVVM ngay từ đầu** (cách đặt tên ViewModel, cách xử lý Command, cách tách Service khỏi ViewModel) để tránh tình trạng code-behind lộn xộn — rủi ro thường gặp khi đội mới chuyển từ WinForms sang WPF là viết code trực tiếp trong code-behind thay vì tuân thủ MVVM, khiến lợi ích của kiến trúc này không phát huy được.
- SRS hiện đang dùng cụm từ "WinForms" tại các mục 1.2, 2.1, 5.2 và bảng NFR mục 4 (Khả năng phục hồi) — cần cập nhật đồng bộ sang "ứng dụng trạm (WPF)" sau khi ADR này được chấp thuận, để tránh nhầm lẫn giữa tài liệu nghiệp vụ và quyết định kiến trúc thực tế.

## Ghi chú triển khai

- Áp dụng mẫu kiến trúc **MVVM** xuyên suốt: View (XAML) chỉ chứa khai báo giao diện và binding; ViewModel chứa trạng thái và logic điều phối; tránh đưa logic nghiệp vụ vào code-behind.
- Tách riêng lớp **Service** đảm nhiệm gọi API (REST) và lắng nghe sự kiện SignalR, để ViewModel chỉ phụ thuộc vào interface của Service (không gọi trực tiếp HttpClient/HubConnection) — giúp dễ kiểm thử ViewModel độc lập và dễ thay đổi tầng giao tiếp sau này.
- Có thể cân nhắc dùng thư viện hỗ trợ MVVM nhẹ (ví dụ CommunityToolkit.Mvvm) để giảm boilerplate (INotifyPropertyChanged, RelayCommand) nếu phù hợp với quy mô đội và tốc độ triển khai — quyết định cụ thể để lại cho giai đoạn thiết kế chi tiết/khởi tạo dự án.
- Vì mỗi trạm gộp nhiều công đoạn trên 1 PC phải chạy nhiều tiến trình riêng biệt (mục 6, quy tắc 7), mỗi màn hình nên là 1 ứng dụng WPF độc lập (không phải nhiều cửa sổ trong cùng 1 process), mỗi ứng dụng chỉ kết nối đúng 1 thiết bị scan/Arduino tương ứng với công đoạn của nó.
