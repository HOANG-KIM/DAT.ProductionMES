# ADR-002: Lựa chọn React (Vite + TypeScript) cho Web Admin

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
12/08/2026

## Bối cảnh (Context)

US-01 → US-06 và US-22 đã hoàn thành ở tầng backend (Controller → Service → Repository) cho các danh mục Line, Stage, WorkStation, ProductionPlan, ProductionPlanStage, User + JWT auth. Backlog (US-01 AC1, AC4) mô tả Admin thao tác qua "màn hình quản lý danh mục"/"giao diện cấu hình", nhưng chưa từng chốt nền tảng UI cho việc này. Hiện tại cách duy nhất để gọi các API trên là Swagger UI — công cụ dev/test, không phải công cụ vận hành hàng ngày cho Admin/Tổ trưởng/Ban quản lý.

Đây là bài toán khác với ADR-001 (client trạm WPF):
- Người dùng là Admin/Tổ trưởng/Ban quản lý, thao tác từ máy văn phòng, không phải công nhân vận hành tại kiosk xưởng.
- Nhu cầu chính là CRUD danh mục (Line/Stage/WorkStation/ProductionPlan/User), sắp xếp trình tự công đoạn (US-03, cần kéo-thả), và về sau là báo cáo/xuất Excel (Giai đoạn 6 của backlog) — không có yêu cầu real-time dày đặc hay nhiều trạng thái động như màn hình scan tại trạm.
- Không có ràng buộc tiến trình/thiết bị vật lý (scan/Arduino) như trạm làm việc.
- Truy cập qua trình duyệt từ nhiều máy văn phòng khác nhau, không cần cài đặt riêng từng máy như ứng dụng desktop.

Ràng buộc đã biết: người phát triển đã có kinh nghiệm JavaScript và xác nhận có thể tự tin dùng kiến trúc SPA (Single Page Application) thay vì cần một giải pháp thuần C# (Blazor/Razor Pages) để tránh học công nghệ mới — khác bối cảnh ADR-001, nơi giảm thiểu đường học là lý do chính chọn WPF.

## Quyết định (Decision)

Chọn **React 18 + TypeScript**, dựng bằng **Vite**, làm nền tảng SPA cho Web Admin — gọi REST API hiện có của `ProductionMES.Api` qua HTTP, theo đúng nguyên tắc tách bạch client đã áp dụng ở ADR-001: mọi client (WPF trạm, Web Admin) đều là consumer của cùng một tầng API/Service, không có đường tắt bypass business rule hay Authorization.

Stack cụ thể:
- **Vite** — build tool, dev server.
- **TypeScript** — type safety khi map DTO từ backend sang phía client.
- **Ant Design** — bộ component dựng sẵn cho CRUD (Table, Form, Tree, kéo-thả).
- **React Router** — điều hướng giữa các màn hình danh mục.
- **TanStack Query (React Query)** — gọi/cache dữ liệu từ API, thay thế state quản lý server-state thủ công.
- **Axios** — HTTP client, gắn JWT Bearer token qua interceptor.

## Lý do (Rationale)

1. **Không phát sinh chi phí học công nghệ mới.** Đội đã biết JavaScript/React, khác với tình huống WPF ở ADR-001 nơi đường học là rủi ro chính cần cân nhắc. Việc chọn đúng công nghệ đội đã quen giúp triển khai nhanh và giảm rủi ro chất lượng ở giai đoạn đầu.

2. **Nhu cầu UI không đòi hỏi kiến trúc real-time nặng.** Khác màn hình trạm (SignalR, nhiều trạng thái động, lý do chính chọn WPF ở ADR-001), Web Admin chủ yếu là CRUD form + bảng dữ liệu, cập nhật theo thao tác người dùng (không cần tự động cập nhật liên tục theo sự kiện production). Kiến trúc SPA gọi REST API là đủ, không cần ràng buộc như Blazor Server (kết nối SignalR thường trực).

3. **Phù hợp mô hình truy cập của người dùng mục tiêu.** Admin/Tổ trưởng/Ban quản lý dùng từ nhiều máy văn phòng khác nhau — ứng dụng web (chỉ cần trình duyệt) thuận tiện hơn ứng dụng desktop phải cài đặt/cập nhật riêng từng máy.

4. **Giữ đúng ranh giới kiến trúc đã thiết lập.** Web Admin là 1 project độc lập, không nằm trong solution .NET, không reference `Application`/`Infrastructure` — chỉ gọi API qua HTTP, cùng nguyên tắc đã áp dụng cho `Station.Wpf`.

## Phương án khác đã xem xét (Alternatives Considered)

**Blazor Server**
- *Ưu điểm*: C# thuần, không cần đội học JS; tận dụng SignalR sẵn có cho khả năng mở rộng dashboard real-time sau này.
- *Vì sao không chọn*: đội đã biết JS và ưu tiên dùng React; hơn nữa Blazor Server yêu cầu duy trì kết nối SignalR thường trực — chi phí kiến trúc không cần thiết cho một công cụ CRUD nội bộ.

**Razor Pages / MVC**
- *Ưu điểm*: C# thuần, mô hình request/response đơn giản, ổn định, không cần chạy thêm hạ tầng real-time.
- *Vì sao không chọn*: tương tác kiểu kéo-thả (US-03 reorder công đoạn) và trải nghiệm SPA mượt cần thêm JS thủ công đáng kể; đội đã có sẵn kinh nghiệm React nên không tận dụng được lợi thế "không cần học JS" của phương án này.

**Vue 3**
- *Ưu điểm*: mô hình template + `v-model` gần với tư duy data-binding MVVM (XAML) mà đội đã quen từ WPF, đường học nhẹ hơn cho người mới viết SPA lần đầu.
- *Vì sao không chọn*: đội xác nhận đã biết JS và có thể tự tin dùng React ngay, nên lợi thế "dễ học" của Vue không phải yếu tố quyết định; ưu tiên hệ sinh thái/thư viện lớn hơn của React.

## Hệ quả (Consequences)

**Tích cực**
- Tách bạch hoàn toàn tầng UI khỏi backend .NET — thay đổi/redeploy Web Admin không ảnh hưởng `Api`/`Station.Wpf`.
- Hệ sinh thái React lớn, nhiều thư viện sẵn có (Ant Design, TanStack Query) giúp triển khai CRUD nhanh.
- Nếu sau này cần thêm ứng dụng web khác (vd. dashboard cho Ban quản lý), có thể tái sử dụng cùng stack, cùng quy ước.

**Tiêu cực / Rủi ro cần lưu ý**
- Thêm một hệ sinh thái công nghệ hoàn toàn khác (Node.js/npm) vào repo vốn thuần .NET — cần quy trình build/CI riêng, không dùng chung `dotnet build`.
- JWT lưu ở phía client (trình duyệt) để gọi API — cần lưu ý rủi ro XSS nếu lưu ở `localStorage`; cần thống nhất chiến lược lưu token khi triển khai chi tiết (xem `web-admin/CLAUDE.md`).
- Trùng lặp một phần logic validate giữa FluentValidation (backend) và validate phía client (React Hook Form/Zod) — cần đối chiếu thủ công khi business rule thay đổi, không có cơ chế đồng bộ tự động giữa 2 tầng.
- Web Admin không dùng chung `ProductionMES.sln`; các lệnh `dotnet build`/`dotnet test` ở `CLAUDE.md` gốc không bao gồm project này.

## Ghi chú triển khai

- Vị trí project: thư mục `web-admin/` tại gốc repo (ngang hàng `src/`), không nằm trong `ProductionMES.sln`.
- Quy ước chi tiết (cấu trúc thư mục, đặt tên, pattern gọi API, quản lý state, xử lý auth...) đặt tại `web-admin/CLAUDE.md` — tự động nạp khi làm việc trong thư mục này.
- Web Admin chỉ gọi API hiện có của `ProductionMES.Api` qua HTTP (base URL cấu hình qua biến môi trường), không truy cập DB hay reference project .NET nào.
