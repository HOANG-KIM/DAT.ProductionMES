# ADR-007: MaterialDesignInXaml (Light theme) + quy ước màu dùng chung cho Station.Wpf, loại trừ AndonBoardWindow

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
17/08/2026

## Bối cảnh (Context)

`Station.Wpf` ban đầu là WPF thuần, không dùng UI framework/thư viện control nào — mỗi Page/Window tự khai hex màu riêng (`#161B23`, `#8D96A8`, `#5C6577`...) trực tiếp trong XAML, lặp lại giống nhau ở 7 file (`MainWindow`, `HomePage`, `PlanSettingsPage`, `PlanSelectionPage`, `LineStageSequencePage`, `LoginDialog`, `AndonBoardWindow`). Nền tối gây khó đọc, và không có 1 nguồn màu duy nhất khiến việc đồng bộ giao diện giữa các màn hình dễ lệch.

Các màn hình này chia làm 2 nhóm khác bản chất:
1. **Màn hình cấu hình của Tổ trưởng** (`HomePage`, `PlanSettingsPage`, `PlanSelectionPage`, `LineStageSequencePage`, `LoginDialog`, `MainWindow`) — thao tác nhập liệu/form thông thường, không có yêu cầu thiết kế đặc thù.
2. **`AndonBoardWindow`** — bảng hiển thị PLAN/ACTUAL/BALANCE và popup kết quả scan OK/NG/WAITING, đã có mockup riêng chốt với người dùng ngày 13/08/2026 (dựng theo ảnh bảng Andon thực tế `Documents/Layout WPF/Capture.PNG`, `OK-NG notify.PNG` — xem memory `station-wpf-dashboard-mockup`), triển khai đầy đủ ở US-07/US-09 (chưa làm, hiện chỉ là placeholder).

## Quyết định (Decision)

1. Dùng package **MaterialDesignThemes** (NuGet, `MaterialDesignThemes` — kéo theo `MaterialDesignColors`) làm UI framework cho `Station.Wpf`, gắn ở `App.xaml` bằng `materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Blue" SecondaryColor="Amber"` + `MaterialDesign2.Defaults.xaml`. Việc này tự động áp style Material cho toàn bộ control WPF chuẩn (Button, TextBox, ComboBox, DataGrid, CheckBox, DatePicker...) mà không cần sửa từng control trong Page — chỉ cần bỏ hex hard-code và merge theme.
2. **Light theme** áp dụng cho toàn bộ nhóm màn hình Tổ trưởng (mục Bối cảnh, nhóm 1) — nền sáng/chữ tối, dễ đọc dưới ánh đèn xưởng, tương phản tốt cho thao tác cảm ứng.
3. Tạo `Themes/AppColors.xaml` làm **quy ước màu dùng chung duy nhất** cho nhóm màn hình Tổ trưởng — không hard-code hex trong Page/Window, luôn tham chiếu `StaticResource`:
   - `TextSecondaryBrush` (#4B5563), `TextMutedBrush` (#9CA3AF) — chữ phụ/chú thích.
   - `SurfaceBrush` (#FFFFFF), `DividerBrush` (#E0E0E0) — nền/viền khối card nổi trên nền trang.
   - `StatusOkBrush` (#2E7D32), `StatusNgBrush` (#C62828), `StatusWarningBrush` (#EF6C00) — màu ngữ nghĩa nghiệp vụ, dùng thống nhất mọi nơi hiển thị trạng thái scan/kế hoạch.
   - Màu nền/chữ mặc định (`MaterialDesignPaper`, `MaterialDesignBody`) lấy thẳng từ theme MaterialDesignInXaml, không định nghĩa lại.
4. **`AndonBoardWindow` KHÔNG áp dụng Light theme và KHÔNG dùng `Themes/AppColors.xaml`** — giữ nguyên hex cục bộ, độc lập với quy ước màu ở mục 3. Khi triển khai đầy đủ US-07/US-09, màn hình này phải bám theo mockup Andon board thực tế đã chốt (nền tối, màu trạng thái động OK/NG/WAITING đổi cả header lẫn body popup — xem memory `station-wpf-dashboard-mockup`), không phải theo bảng màu ở mục 3.

## Lý do (Rationale)

1. **MaterialDesignInXaml miễn phí, thuần WPF, tương thích MVVM/`CommunityToolkit.Mvvm` đang dùng** — không cần viết `ControlTemplate` từ đầu cho từng control, và merge theme 1 lần ở `App.xaml` áp dụng ngay cho toàn bộ Page hiện có lẫn Page mới sau này.
2. **Light theme cho nhóm Tổ trưởng**: đây là màn hình cấu hình/nhập liệu thông thường, không có yêu cầu thiết kế đặc thù nào đòi hỏi nền tối — nền sáng là lựa chọn phổ biến, dễ đọc hơn dưới ánh đèn xưởng so với nền tối trước đó.
3. **`Themes/AppColors.xaml` là nguồn màu duy nhất**: đổi màu hệ thống chỉ cần sửa 1 file, tránh lặp/lệch hex như tình trạng trước ADR này.
4. **Loại trừ `AndonBoardWindow`**: đây là bảng hiển thị nhìn từ xa suốt ca làm việc, đã có mockup riêng được người dùng duyệt trước khi có quyết định UI framework này (13/08/2026, trước 17/08/2026) — áp Light theme chung lên sẽ phá vỡ thiết kế đã chốt và không phù hợp ngữ cảnh nhìn xa/độ tương phản khác với màn hình cấu hình cầm tay/chạm gần.

## Phương án khác đã xem xét (Alternatives Considered)

**Tự viết `ResourceDictionary` theme riêng, không dùng thư viện ngoài**
- *Ưu điểm*: không thêm dependency, toàn quyền kiểm soát.
- *Vì sao không chọn*: tốn công viết lại `ControlTemplate` cho Button/TextBox/ComboBox/DataGrid... từ đầu để có hiệu ứng/độ hoàn thiện tương đương, trong khi MaterialDesignInXaml đã có sẵn và ổn định.

**Áp Light theme luôn cho cả `AndonBoardWindow`**
- *Ưu điểm*: đồng nhất tuyệt đối giao diện toàn ứng dụng, không cần phân biệt 2 bộ quy ước màu.
- *Vì sao không chọn*: vi phạm mockup Andon board đã chốt với người dùng (nền tối, xem từ xa cả ca làm việc) — người dùng xác nhận rõ khi rà lại (17/08/2026): Light theme không áp dụng cho Andon board, màn này dùng theo mockup riêng.

## Hệ quả (Consequences)

**Tích cực**
- Giao diện Tổ trưởng đồng bộ, dễ đọc hơn nền tối trước đó; đổi màu hệ thống chỉ sửa `Themes/AppColors.xaml`.
- Không tốn công viết lại control template; các Page mới sau này tự động thừa hưởng style Material khi thêm control chuẩn WPF.

**Tiêu cực / Rủi ro cần lưu ý**
- Style `Button` mặc định của MaterialDesignThemes (`MaterialDesignRaisedButton`) ép cứng `Height="32"` và clip nội dung theo đúng chiều cao đó (qua `wpf:Ripple.Clip`) — Button có nội dung nhiều dòng/padding lớn (như 3 nút tile ở `HomePage`) phải tự set `Height="Auto"` để tránh chữ bị cắt/mất, không tự phát hiện được lúc build (XAML compile không báo lỗi, chỉ thấy khi chạy).
- 2 bộ quy ước màu song song trong cùng project (`Themes/AppColors.xaml` cho nhóm Tổ trưởng, hex cục bộ trong `AndonBoardWindow` cho Andon board) — dev mới cần đọc ADR này trước khi sửa màu ở màn hình nào, tránh nhầm lẫn dùng chung 1 bảng màu cho cả 2 nhóm.
- Khi triển khai US-07/US-09 (Andon board thật), cần đối chiếu lại mockup/memory `station-wpf-dashboard-mockup` thay vì suy đoán màu từ `Themes/AppColors.xaml`.

## Ghi chú triển khai

- File liên quan: `App.xaml` (merge `BundledTheme` + `MaterialDesign2.Defaults.xaml`), `Themes/AppColors.xaml` (brush dùng chung), `ProductionMES.Station.Wpf.csproj` (`PackageReference MaterialDesignThemes`).
- Khi thêm Page/Window mới thuộc nhóm Tổ trưởng: đặt `Background="{DynamicResource MaterialDesignPaper}"` ở root, dùng `{StaticResource ...}` trỏ tới brush trong `Themes/AppColors.xaml` cho màu ngữ nghĩa, không hard-code hex mới.
- Khi implement `AndonBoardWindow` thật (US-07/US-09): không merge `Themes/AppColors.xaml` vào màn này; lấy màu trực tiếp từ mockup đã duyệt.
- Button với nội dung cao hơn 1 dòng hoặc padding dọc lớn: luôn set `Height="Auto"` tường minh để tránh bug clip mô tả ở mục Hệ quả.
