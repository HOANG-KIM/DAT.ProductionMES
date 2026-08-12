# ADR-005: Auth cho Station.Wpf — API Key theo trạm (scan thường) + Bearer/Refresh Token (Supervisor nâng quyền)

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
12/08/2026

## Bối cảnh (Context)

ADR-003 chốt HttpOnly Cookie + Refresh Token cho `web-admin` vì lo ngại XSS đọc token từ `localStorage`/JS trên trình duyệt khi hệ thống mở ra internet công cộng. `Station.Wpf` là ứng dụng desktop tại trạm làm việc, chuẩn bị triển khai US-07/US-08 (scan tem), hiện chưa có bất kỳ dòng code auth nào (scaffold trống).

`Station.Wpf` có 2 luồng nghiệp vụ với đặc điểm người dùng khác hẳn nhau:

1. **Luồng scan thường (FR-07/FR-08, US-07/US-08)**: Operator vận hành trạm **không đăng nhập cá nhân** — công nhân chỉ thao tác scan liên tục suốt ca, không có bước nhập username/password trước mỗi lượt scan. Nhu cầu thật sự là xác thực **trạm** (`WorkStation`) đang gọi API, không phải xác thực người.
2. **Luồng Supervisor nâng quyền tại trạm** (đã mô tả ở `CLAUDE.md` gốc: cấu hình `ProductionPlan`/`ProductionPlanStage`, và sau này US-19 mở khóa rework, US-12 xác nhận NG do Arduino): Tổ trưởng đăng nhập bằng tài khoản cá nhân ngay tại màn hình trạm, thao tác có tính nhạy cảm và cần audit theo đúng người.

Áp dụng nguyên xi cơ chế Cookie của ADR-003 cho `Station.Wpf` không phù hợp: cookie HttpOnly được thiết kế để trình duyệt tự động đính kèm và ngăn JavaScript đọc, nhưng `Station.Wpf` không có DOM/JS — lớp tấn công XSS mà ADR-003 nhắm tới không tồn tại ở đây. Đồng thời quản lý `CookieContainer` thủ công trong `HttpClient` desktop không mang lại lợi ích tương xứng so với chi phí phức tạp thêm (CORS credentials, Antiforgery/CSRF token, cookie `Path` theo scope) — các cơ chế đó được thiết kế riêng cho ngữ cảnh trình duyệt.

## Quyết định (Decision)

**1. Luồng scan thường — API Key theo trạm, không cá nhân hóa:**
- Mỗi `WorkStation` được cấp 1 API key riêng (chuỗi random đủ dài). Server chỉ lưu **hash** của key (cùng nguyên tắc không lưu giá trị thô như `RefreshToken.TokenHash` ở ADR-003), do Admin cấp khi tạo/cấu hình trạm (US-04).
- Giá trị thô của API key lưu trong **file cấu hình cục bộ tại trạm** — cùng pattern Options đã dùng cho `ArduinoTimeoutSeconds`/`NgModeTimeoutSeconds` (đọc từ `appsettings.json` cục bộ của từng trạm, không phải cấu hình tập trung ở server).
- Mọi request gọi `POST api/v1/scans` đính kèm header `X-Station-Api-Key`. Server hash giá trị nhận được rồi so khớp với hash đã lưu cho đúng `WorkStationId`, đồng thời đối chiếu API key đó thuộc đúng `WorkStationId` gửi trong request body — tránh trạm A dùng key của mình gọi giả danh trạm B.
- Entity mới `StationApiKey` (tách riêng khỏi `WorkStation`, không chỉ thêm field) để giữ lịch sử key cũ đã thu hồi khi xoay vòng.
- Không có khái niệm hết hạn ngắn theo phiên (khác access token 15 phút của ADR-003) — API key có hiệu lực dài hạn cho tới khi Admin chủ động thu hồi/cấp lại (nghi ngờ rò rỉ, thay thiết bị trạm...), qua thao tác tương tự deactivate.
- Xác thực bằng `AuthenticationScheme` riêng (`"StationApiKey"`), **không** đi qua hệ permission `Resource.Action` của ADR-004 — vì không có `User` nào gắn với request này để tra `RolePermission`. `ScansController.Create` dùng `[Authorize(AuthenticationSchemes = "StationApiKey")]`, không cần thêm `PermissionResource.Scan`/`PermissionAction.Create` vào bảng `Permission`.

**2. Luồng Supervisor nâng quyền tại trạm — Bearer Access Token + Refresh Token (không phải Cookie):**
- Giữ nguyên hạ tầng đã có ở ADR-003: access token ngắn hạn (15 phút) + refresh token dài hạn (7 ngày, thu hồi được, rotation + reuse detection) — chỉ đổi **kênh vận chuyển**: trả token trong JSON body thay vì `Set-Cookie`.
- `Station.Wpf` tự lưu access token trong bộ nhớ tiến trình (không ghi ra file/registry), tự gắn header `Authorization: Bearer {token}` cho mọi request cần quyền Supervisor.
- Không cần Antiforgery/CSRF token — CSRF lợi dụng trình duyệt tự động gửi cookie kèm request nền, không áp dụng khi không dùng cookie.
- Không cần cấu hình CORS riêng cho `Station.Wpf` — desktop client không bị same-origin policy chi phối.
- Refresh token dùng chung bảng `RefreshToken` với `web-admin` (phân biệt qua `UserId`), rotation/reuse detection dùng lại nguyên logic đã có ở ADR-003, chỉ khác kênh vận chuyển.
- `Scan.View` (tra cứu lịch sử scan, US-10) vẫn đi qua permission động (ADR-004) như bình thường, cấp cho `Supervisor`/`Admin`/`Manager` — vì đây là người dùng thật đã đăng nhập.

**3. Tách riêng endpoint đăng nhập cho `Station.Wpf`:**
- `web-admin` giữ nguyên `POST api/v1/auth/login` trả cookie (ADR-003), không đổi.
- `Station.Wpf` (luồng Supervisor) dùng endpoint riêng `POST api/v1/auth/station-login` — validate username/password giống `login`, nhưng trả token trong JSON body thay vì set cookie. Tách riêng để không làm phức tạp thêm logic `AuthController.Login` hiện có (vốn đã gắn chặt với cookie/CSRF theo ADR-003) bằng nhánh if/else theo loại client.

## Lý do (Rationale)

1. **Đúng threat model của từng loại client** — Cookie HttpOnly giải quyết XSS (rủi ro của trình duyệt); Bearer-in-memory là pattern chuẩn cho desktop client (rủi ro chính là máy trạm bị chiếm quyền/đọc bộ nhớ tiến trình — nằm ngoài khả năng phòng thủ của bất kỳ cơ chế token nào, kể cả Cookie).
2. **API key theo trạm đúng bản chất "trạm là đơn vị xác thực", không phải người** — tránh ép Operator qua bước đăng nhập không cần thiết mỗi ca, đúng yêu cầu nghiệp vụ (Operator không đăng nhập cá nhân cho luồng scan thường).
3. **Tận dụng lại hạ tầng refresh token đã xây ở ADR-003** cho luồng Supervisor — không phát sinh entity/logic rotation mới, chỉ đổi kênh vận chuyển.
4. **Nhất quán với pattern cấu hình cục bộ theo trạm đã có** (timeout Arduino/NG) — API key trạm cũng nằm trong cùng file cấu hình cục bộ, không cần thêm cơ chế quản lý bí mật (secret management) mới.
5. **Giới hạn đúng blast radius khi rò rỉ** — mỗi API key chỉ có hiệu lực cho đúng 1 `WorkStationId`, không phải quyền toàn hệ thống; rò rỉ 1 trạm không ảnh hưởng các trạm khác.

## Phương án khác đã xem xét (Alternatives Considered)

**Dùng chung Cookie như web-admin cho cả Station.Wpf**
- *Ưu điểm*: 1 cơ chế auth duy nhất cho toàn hệ thống, không phải học/maintain 2 pattern.
- *Vì sao không chọn*: `HttpClient` desktop phải tự quản lý `CookieContainer` thủ công, mất lợi thế "trình duyệt tự gửi" mà ADR-003 tận dụng; phải thêm CORS/Antiforgery không cần thiết cho ngữ cảnh không có XSS.

**Operator cũng đăng nhập cá nhân bằng Bearer token (không dùng API key trạm)**
- *Ưu điểm*: đồng nhất hoàn toàn với luồng Supervisor, mọi request đều gắn được `UserId`.
- *Vì sao không chọn*: trái với yêu cầu nghiệp vụ đã xác nhận (Operator không đăng nhập cá nhân); buộc thêm UI đăng nhập vào luồng scan liên tục tốc độ cao, cản trở thao tác.

**1 loại token duy nhất cho cả 2 luồng** (vd. chỉ dùng API key trạm, Supervisor cũng "nâng quyền" bằng 1 key khác thay vì đăng nhập cá nhân)
- *Ưu điểm*: đơn giản nhất về số lượng cơ chế.
- *Vì sao không chọn*: mất khả năng audit theo đúng cá nhân Tổ trưởng (bắt buộc cho US-19 mở khóa rework — "ai duyệt, thời điểm nào"), và mất cơ chế thu hồi/hết hạn ngắn hạn cần cho thao tác nhạy cảm.

**Đổi API key lấy access token tạm qua 1 endpoint kiểu `POST /api/v1/stations/{id}/token`**
- *Ưu điểm*: thống nhất mọi request về 1 dạng Bearer token duy nhất.
- *Vì sao không chọn*: API key trạm không có nhu cầu hết hạn ngắn hạn/rotate theo phiên như access token của người dùng; thêm bước đổi token tăng round-trip không cần thiết mỗi lần khởi động `Station.Wpf` mà không mang lại lợi ích bảo mật tương xứng (rủi ro rò rỉ đã giới hạn đúng 1 trạm).

## Hệ quả (Consequences)

**Tích cực**
- Đúng nhu cầu nghiệp vụ thực tế: scan thường nhanh, không bị cản trở bởi bước đăng nhập; thao tác Tổ trưởng vẫn có audit trail đầy đủ theo đúng cá nhân.
- Tái sử dụng tối đa hạ tầng refresh token đã có ở ADR-003, giảm code mới cho luồng Supervisor.
- Rò rỉ 1 API key chỉ ảnh hưởng đúng 1 trạm, không lan ra toàn hệ thống.

**Tiêu cực / Rủi ro cần lưu ý**
- Thêm 1 loại credential mới (API key trạm) cần quản lý vòng đời riêng (cấp/thu hồi/xoay vòng) — `web-admin` chưa có UI cho việc này, cần bổ sung vào US-04 hoặc 1 story riêng trước/song song khi code US-07/08.
- API key trạm là bí mật dài hạn lưu trên máy trạm (file cấu hình cục bộ) — rủi ro lộ nếu máy trạm bị truy cập trái phép; giảm thiểu bằng cách mỗi key chỉ có hiệu lực cho đúng 1 `WorkStationId`.
- 2 endpoint login riêng biệt (`login` cookie-based cho web-admin, `station-login` bearer-based cho Station.Wpf) cần đồng bộ thủ công nếu sau này đổi logic validate chung (vd. rule khóa tài khoản sau N lần sai) — cần lưu ý khi sửa `AuthService`.
- Thêm 1 `AuthenticationScheme` mới (`"StationApiKey"`) chạy song song với `"Bearer"` hiện có trong pipeline authentication của `Program.cs`.

## Ghi chú triển khai

- Entity `StationApiKey`: `Id`, `WorkStationId`, `KeyHash`, `CreatedAtUtc`, `RevokedAtUtc` (nullable) — hỗ trợ xoay vòng, giữ lịch sử key cũ đã thu hồi thay vì ghi đè 1 field trên `WorkStation`.
- `ScansController.Create`: `[Authorize(AuthenticationSchemes = "StationApiKey")]`, đọc `WorkStationId` từ danh tính trạm đã xác thực (không chỉ từ request body) để đối chiếu chống giả danh.
- Entity `Scan` (US-07/08) **không có field `UserId`** — luồng scan thường không gắn với người dùng cụ thể nào. Nếu US-18/US-19 sau này cần ghi nhận "người xác nhận NG"/"người mở khóa rework", đó là hành động của Supervisor đã đăng nhập Bearer token theo cơ chế ADR này — dùng field riêng đúng ngữ cảnh của US-18/19, không tái dùng/thêm lại `UserId` chung vào `Scan`.
- `AuthController` bổ sung `POST api/v1/auth/station-login` (trả token trong JSON body) bên cạnh `POST api/v1/auth/login` (trả cookie, giữ nguyên cho web-admin) — cùng validate credentials qua `AuthService`, khác ở bước trả kết quả.
- `Scan.View` vẫn seed permission động (ADR-004) bình thường cho `Supervisor`/`Admin`/`Manager`; **không** seed `Scan.Create` vào bảng `Permission` vì endpoint đó không đi qua permission theo role.
