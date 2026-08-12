# ADR-003: Chuyển JWT sang HttpOnly Cookie + Refresh Token thu hồi được

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
12/08/2026

## Bối cảnh (Context)

`API-Conventions.md` (mục 7) ban đầu ghi nhận cơ chế JWT dạng **Bearer token thuần**: server trả token trong JSON body (`LoginResponse.Token`), client (`web-admin`) tự lưu (dự kiến `localStorage`) và tự gắn header `Authorization: Bearer`. Quyết định này chấp nhận được khi hệ thống chỉ chạy trong LAN nhà máy.

Thông tin mới: hệ thống **sẽ mở ra public internet** trong tương lai, không chỉ chạy nội bộ LAN. Điều này thay đổi hẳn mức độ rủi ro cần chấp nhận:

- **Rủi ro XSS đọc trộm token từ `localStorage`** không còn là rủi ro lý thuyết chấp nhận được — trên internet công cộng, bề mặt tấn công XSS (thư viện bên thứ ba, extension trình duyệt độc hại...) lớn hơn nhiều so với mạng nội bộ kiểm soát được.
- Token JWT thuần (`ExpiryMinutes = 480`, tức 8 giờ) không có cách nào **thu hồi giữa chừng** (vd. khi Admin khóa tài khoản, khi phát hiện token bị lộ) — token vẫn hợp lệ tới khi hết hạn tự nhiên dù đã bị vô hiệu hóa ở DB.

## Quyết định (Decision)

1. Lưu token trong **cookie `HttpOnly` + `Secure` + `SameSite=Strict`** thay vì trả trong JSON body — JavaScript phía client không đọc được token, loại bỏ hoàn toàn đường tấn công "XSS đọc `localStorage`".
2. Tách **access token ngắn hạn** (mặc định 15 phút, cấu hình qua `Jwt:AccessTokenExpiryMinutes`) và **refresh token dài hạn** (mặc định 7 ngày, `Jwt:RefreshTokenExpiryDays`), refresh token **lưu server-side** (entity `RefreshToken`, chỉ lưu hash — không lưu giá trị thô) để có thể thu hồi.
3. **Rotation + phát hiện tái sử dụng (reuse detection)**: mỗi lần refresh, token cũ bị thu hồi ngay và cấp token mới; nếu một refresh token đã bị thu hồi lại được gửi lên lần nữa (dấu hiệu bị đánh cắp và dùng song song với chủ tài khoản thật), toàn bộ refresh token đang hoạt động của user đó bị thu hồi, buộc đăng nhập lại.
4. **Chống CSRF** bằng ASP.NET Core `IAntiforgery`: client lấy CSRF token qua `GET api/v1/auth/csrf`, gắn vào header `X-CSRF-TOKEN` cho mọi request `POST`/`PUT`; server validate token này trước khi xử lý (bổ sung cho `SameSite=Strict` như lớp phòng thủ thứ 2, vì `SameSite` một mình không chống được mọi biến thể CSRF trên các trình duyệt cũ).
5. **CORS** khai báo origin cụ thể của `web-admin` (không dùng `*`) kèm `AllowCredentials()` — bắt buộc khi dùng cookie cho request cross-origin giữa `web-admin` (origin riêng) và `ProductionMES.Api`.

## Lý do (Rationale)

1. **HttpOnly cookie loại bỏ hẳn class lỗi XSS-đánh-cắp-token**, thay vì chỉ giảm thiểu — đây là khác biệt chất lượng quan trọng khi hệ thống ra internet công cộng, nơi không thể kiểm soát hết mọi thư viện/phụ thuộc phía client theo thời gian.
2. **Access token ngắn hạn giảm cửa sổ rủi ro** nếu token bị lộ qua kênh khác (vd. log, lỗi cấu hình); refresh token dài hạn nhưng **thu hồi được** giải quyết đúng vấn đề JWT thuần không xử lý được (không thể vô hiệu hóa giữa chừng).
3. **Reuse detection** là biện pháp chuẩn cho refresh token rotation (khuyến nghị phổ biến trong OAuth2/OWASP) — phát hiện sớm khi có dấu hiệu token bị đánh cắp thay vì chỉ phòng ngừa thụ động.
4. **Antiforgery + SameSite=Strict là 2 lớp phòng thủ CSRF độc lập** — `SameSite=Strict` là lớp chính (chặn hầu hết trình duyệt hiện đại), Antiforgery token là lớp dự phòng cho trường hợp `SameSite` bị bỏ qua/không hỗ trợ đầy đủ.

## Phương án khác đã xem xét (Alternatives Considered)

**Giữ Bearer token + `localStorage`, chỉ thêm cảnh báo tài liệu**
- *Ưu điểm*: không cần sửa gì, đơn giản nhất.
- *Vì sao không chọn*: chấp nhận được khi còn giới hạn trong LAN, nhưng không phù hợp khi hệ thống ra internet công cộng — mâu thuẫn trực tiếp với yêu cầu "security phải đảm bảo" vừa đặt ra.

**HttpOnly Cookie nhưng không có Refresh Token (chỉ 1 access token dài hạn trong cookie)**
- *Ưu điểm*: đơn giản hơn, không cần entity/bảng mới, không cần logic rotation.
- *Vì sao không chọn*: quay lại đúng vấn đề "không thu hồi được giữa chừng" của JWT thuần — cookie chỉ giải quyết XSS, không giải quyết được nhu cầu thu hồi khi Admin khóa tài khoản hoặc nghi ngờ lộ token.

**Chỉ dựa vào `SameSite=Strict`, không thêm Antiforgery token**
- *Ưu điểm*: ít code hơn, không cần endpoint `/auth/csrf` riêng.
- *Vì sao không chọn*: `SameSite=Strict` là phòng thủ tốt nhưng không phải tuyệt đối (phụ thuộc hỗ trợ trình duyệt, có edge case với điều hướng top-level). Vì đây là hệ thống sẽ public internet và cần "đảm bảo" bảo mật, chấp nhận thêm 1 endpoint đổi lấy lớp phòng thủ thứ 2 độc lập là hợp lý.

## Hệ quả (Consequences)

**Tích cực**
- Loại bỏ hoàn toàn rủi ro XSS đọc token phía client.
- Có khả năng thu hồi phiên đăng nhập giữa chừng (khóa tài khoản, nghi ngờ lộ token) — điều JWT thuần không làm được.
- Phát hiện được dấu hiệu refresh token bị đánh cắp qua reuse detection.

**Tiêu cực / Rủi ro cần lưu ý**
- Tăng đáng kể độ phức tạp so với Bearer token thuần: thêm entity `RefreshToken` + migration, logic rotation/reuse-detection ở `AuthService`, cấu hình Antiforgery + CORS credentials ở `Program.cs`.
- `web-admin` phải gọi `GET /auth/csrf` trước khi thực hiện request đổi dữ liệu đầu tiên, và phải tự xử lý luồng "access token hết hạn giữa chừng → gọi `/auth/refresh` → thử lại request gốc" ở tầng Axios interceptor — phức tạp hơn so với chỉ gắn 1 header tĩnh.
- Do dùng cookie, `web-admin` và `ProductionMES.Api` cần cấu hình CORS chặt (origin cụ thể + `AllowCredentials`) — không còn linh hoạt gọi API từ origin bất kỳ như Bearer header.
- Cần dọn refresh token hết hạn/đã thu hồi định kỳ (tránh phình bảng `RefreshToken` vô hạn) — chưa thiết kế job dọn dẹp cụ thể trong ADR này, để lại cho giai đoạn vận hành.

## Ghi chú triển khai

- Entity mới `RefreshToken` (Domain): `Id`, `UserId`, `TokenHash` (SHA-256 của giá trị thô, KHÔNG lưu giá trị thô), `ExpiresAtUtc`, `CreatedAtUtc`, `RevokedAtUtc` (nullable), `ReplacedByTokenHash` (nullable, phục vụ truy vết chuỗi rotation).
- Cookie: `access_token` (HttpOnly, Secure, SameSite=Strict, `Path=/`, hết hạn theo `AccessTokenExpiryMinutes`), `refresh_token` (HttpOnly, Secure, SameSite=Strict, `Path=/api/v1/auth`, hết hạn theo `RefreshTokenExpiryDays` — giới hạn `Path` để cookie này chỉ gửi kèm khi gọi đúng nhóm endpoint auth, giảm bề mặt lộ).
- `JwtOptions` đổi `ExpiryMinutes` → `AccessTokenExpiryMinutes` (mặc định 15) + thêm `RefreshTokenExpiryDays` (mặc định 7). Vì DB chưa từng deploy thật, đổi tên field không cần cân nhắc backward-compat.
- `IJwtTokenGenerator` bổ sung sinh refresh token thô (random, không phải JWT) + hàm hash để lưu/so khớp.
- Endpoint mới: `POST api/v1/auth/refresh` (rotate token, đọc `refresh_token` cookie), `POST api/v1/auth/logout` (thu hồi refresh token hiện tại, xóa cả 2 cookie), `GET api/v1/auth/csrf` (cấp CSRF token ban đầu, gọi trước request đổi dữ liệu đầu tiên).
- `LoginResponse` bỏ field `Token`; body trả về chỉ còn thông tin hiển thị (`Username`, `FullName`, `UserRole`, `AccessTokenExpiresAtUtc`) — token không xuất hiện trong JSON body ở bất kỳ endpoint nào.
- Chi tiết hợp đồng API cập nhật đầy đủ tại `Documents/API-Conventions.md` mục 7; quy ước phía `web-admin` (Axios interceptor, xử lý refresh, CSRF header) tại `web-admin/CLAUDE.md`.
