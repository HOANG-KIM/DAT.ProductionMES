# Quy ước API — ProductionMES.Api ↔ web-admin (và các client khác)

Tài liệu này thống nhất hợp đồng (contract) giữa `ProductionMES.Api` (backend) và mọi client gọi qua HTTP (`web-admin`, và về sau `Station.Wpf`). Áp dụng cho toàn bộ endpoint hiện có lẫn endpoint mới. Phần nào phản ánh code hiện tại thì ghi rõ; phần nào là quy ước cho tương lai (chưa implement) thì đánh dấu rõ **[Chưa implement]**.

## 1. API URL convention

- Base path có version: `api/v1/{resource}`.
- Tên resource: **danh từ số nhiều, kebab-case** — vd. `lines`, `stages`, `work-stations`, `production-plans`, `users`.
- Resource lồng nhau dùng đường dẫn cha/con: `api/v1/production-plans/{productionPlanId:int}/stages`.
- Route param có ràng buộc kiểu: `{id:int}` (không dùng `{id}` trần).
- Hành động không phải CRUD chuẩn (không map thẳng vào GET/POST/PUT resource) đặt thành verb ở cuối path, dùng `POST`: `POST api/v1/lines/{id}/deactivate`.
- Cập nhật 1 phần con cụ thể của resource (không phải toàn bộ entity) tách route riêng: `PUT api/v1/users/{id}/role`.
- Bump version (`v2`) khi có breaking change (đổi field bắt buộc, đổi ý nghĩa status code...); thêm field mới/optional không cần bump version.

## 2. HTTP methods

| Method | Dùng khi | Ví dụ |
|---|---|---|
| `GET` | Lấy danh sách/chi tiết, không side-effect | `GET /lines`, `GET /lines/{id}` |
| `POST` | Tạo mới, hoặc hành động không idempotent (login, deactivate) | `POST /lines`, `POST /lines/{id}/deactivate`, `POST /auth/login` |
| `PUT` | Cập nhật — Service quyết định field nào được sửa (có thể chỉ 1 phần field của entity, xem `LineService.UpdateAsync` chỉ sửa Name/Description), không bắt buộc gửi đủ toàn bộ entity như REST PUT thuần | `PUT /lines/{id}`, `PUT /users/{id}/role` |
| `DELETE` | Xóa cứng — **chỉ** dùng cho bản ghi thuần liên kết/cấu hình, không có ý nghĩa lịch sử độc lập (vd. gỡ 1 Stage khỏi ProductionPlan). Với entity chính có dữ liệu lịch sử tham chiếu (Line/Stage/WorkStation/User/ProductionPlan), "xóa" luôn là soft-delete qua `POST {id}/deactivate`, không dùng `DELETE` | `DELETE /production-plans/{id}/stages/{stageId}`, `DELETE /roles/{role}/permissions/{permissionId}` |
| `PATCH` | Không dùng hiện tại — `PUT` theo nghĩa "cập nhật field được phép sửa" đã đủ dùng cho quy mô CRUD hiện có | — |

## 3. JSON naming

- Dùng **camelCase** cho mọi field JSON (mặc định của `System.Text.Json` trong ASP.NET Core, không override) — property C# `PascalCase` tự động map sang JSON `camelCase`. Vd. `LineDto.IsActive` → JSON `"isActive"`.
- Không cấu hình custom naming policy khác — giữ mặc định để tránh lệch giữa Swagger doc và hành vi thật.

## 4. DTO request/response

- Response luôn là DTO (`XxxDto`), **không bao giờ** trả trực tiếp EF Entity ra ngoài (luật đã chốt ở `CLAUDE.md` gốc).
- Đặt tên request theo hành động: `CreateXxxRequest`, `UpdateXxxRequest`, `UpdateXxxRoleRequest`... — không dùng chung 1 request type cho nhiều hành động khác mục đích dù field trùng nhau, để validator (FluentValidation) áp rule riêng từng hành động.
- Phía `web-admin/src/types/`: đặt tên type TypeScript **trùng tên** DTO backend (vd. `LineDto`, `CreateLineRequest`) và khớp tên field — khi backend đổi DTO, cập nhật type tương ứng, không tự suy đoán field (đã ghi ở `web-admin/CLAUDE.md`).

## 5. HTTP status codes

| Status | Khi nào |
|---|---|
| `200 OK` | `GET`/`PUT` thành công |
| `201 Created` | `POST` tạo mới thành công (kèm header `Location` qua `CreatedAtAction`) |
| `204 No Content` | Hành động thành công không trả body (vd. `deactivate`) |
| `400 Bad Request` | Lỗi validate (FluentValidation) |
| `401 Unauthorized` | Chưa đăng nhập / token thiếu, sai, hết hạn |
| `403 Forbidden` | Đã đăng nhập nhưng sai role (`[Authorize(Roles=...)]` chặn) |
| `404 Not Found` | `EntityNotFoundException` (Domain) — không tìm thấy bản ghi |
| `409 Conflict` | `BusinessRuleException` (Domain) — vi phạm quy tắc nghiệp vụ (vd. đã có ProductionPlan active trên Line) |
| `500 Internal Server Error` | Lỗi chưa lường trước — `GlobalExceptionHandler` bắt toàn cục |

## 6. Error response format

Toàn bộ lỗi (400/404/409/500) trả về theo chuẩn **RFC 7807 `ProblemDetails`** — xử lý tập trung ở `GlobalExceptionHandler` (404/409/500) và `FluentValidationActionFilter` (400):

```json
// 404/409/500 — ProblemDetails
{
  "status": 409,
  "title": "Vi phạm quy tắc nghiệp vụ",
  "detail": "Line này đã có 1 kế hoạch sản xuất đang active.",
  "instance": "/api/v1/production-plans"
}

// 400 — ValidationProblemDetails (thêm field errors)
{
  "status": 400,
  "title": "One or more validation errors occurred.",
  "errors": {
    "Name": ["'Name' không được để trống."]
  }
}
```

Quy ước phía `web-admin`: viết 1 hàm chuẩn hóa lỗi dùng chung trong Axios interceptor (`api/`), nhận diện theo `status` để hiển thị đúng (400 → gắn lỗi vào từng field form qua `errors`; 404/409/500 → hiển thị `detail` dạng thông báo chung; 401 → tự động đăng xuất + điều hướng trang login).

## 7. Authentication/JWT

**Đã đổi sang HttpOnly Cookie kể từ ADR-003** (xem `Documents/ADR-003-httponly-cookie-refresh-token.md`) — không còn trả token trong JSON body, không còn gắn `Authorization: Bearer` header. Lý do: hệ thống sẽ mở ra public internet, cần loại bỏ rủi ro XSS đọc token từ `localStorage` và cần khả năng thu hồi phiên đăng nhập giữa chừng.

- **`GET api/v1/auth/csrf`** — gọi trước tiên (khi `web-admin` khởi động, trước request đổi dữ liệu đầu tiên). Set cookie CSRF (`XSRF-TOKEN`, không HttpOnly — JS đọc được để echo lại) và trả về:
  ```json
  { "csrfToken": "..." }
  ```
  Client lưu `csrfToken` ở bộ nhớ (không cần lưu bền), gắn vào header `X-CSRF-TOKEN` cho **mọi request `POST`/`PUT`** (kể cả `login`).

- **`POST api/v1/auth/login`** — body `LoginRequest { username, password }`, kèm header `X-CSRF-TOKEN`. Response **không còn field token** trong body:
  ```json
  { "username": "admin", "fullName": "...", "userRole": "Admin", "accessTokenExpiresAtUtc": "2026-08-13T02:15:00Z" }
  ```
  Server set 2 cookie: `access_token` (HttpOnly, Secure, SameSite=Strict, hết hạn ~15 phút) và `refresh_token` (HttpOnly, Secure, SameSite=Strict, `Path=/api/v1/auth`, hết hạn ~7 ngày).

- **`POST api/v1/auth/refresh`** — không cần body, cookie `refresh_token` tự gửi kèm. Rotate: cấp cặp access/refresh token mới, thu hồi token cũ, set lại cả 2 cookie. Trả cùng shape body như `login`. Nếu `refresh_token` không hợp lệ/đã thu hồi (dấu hiệu bị đánh cắp) → `401`, toàn bộ refresh token của user bị thu hồi phía server, client phải đăng nhập lại.

- **`POST api/v1/auth/logout`** — thu hồi `refresh_token` hiện tại, xóa cả 2 cookie, trả `204`.

- **Mọi request cần xác thực**: không cần gắn header thủ công — cookie `access_token` tự gửi kèm (Axios cần `withCredentials: true`). Chỉ cần gắn header `X-CSRF-TOKEN` cho request `POST`/`PUT`.

- Claim role trong JWT dùng short type `"role"` (`JwtTokenGenerator.RoleClaimType`) — dùng để `[Authorize(Roles=...)]` ở backend; phía client không tự decode token (token không đọc được từ JS do HttpOnly) — role hiển thị UI lấy từ response body `login`/`refresh` (field `userRole`), lưu ở `store/` (chỉ lưu trong bộ nhớ phiên, không phải cookie/localStorage).

- Nhận `401` khi gọi API (không phải `login`) → thử gọi `POST /auth/refresh` **một lần**; nếu refresh cũng `401` → coi như phiên hết hạn thật, xóa session ở `store/`, điều hướng về trang login. Nếu refresh thành công → thử lại request gốc đúng 1 lần (tránh vòng lặp vô hạn nếu backend có lỗi khác gây 401 dai dẳng).

## 8. Authorization/Role

**Đổi sang permission động lưu DB kể từ ADR-004** (xem `Documents/ADR-004-role-permission-dong.md`) — không còn 1 danh sách role hardcode chung cho cả Controller.

- 4 role hệ thống: `Operator`, `Supervisor`, `Admin`, `Manager` (enum `UserRole`, serialize dạng chuỗi — xem mục 10).
- Permission mô hình theo cặp **`Resource.Action`** (vd. `"Line.View"`, `"ProductionPlan.Activate"`), lưu ở bảng `Permission`/`RolePermission` — role nào được làm gì tra cứu từ DB (qua cache), Admin chỉnh qua UI runtime, **không cần deploy lại**.
- **Ngoại lệ break-glass**: API quản lý `User` (`UsersController`) và API quản lý `Permission`/`RolePermission` vẫn hardcode `[Authorize(Roles = "Admin")]`, KHÔNG đi qua permission động — đây là đường quản trị luôn hoạt động được, không thể bị khóa qua chính UI phân quyền.
- `web-admin` chỉ phục vụ role có ít nhất 1 permission hiệu lực (`Operator` luôn bị chặn — vai trò đó thao tác ở `Station.Wpf`, không có permission nào cả). `RouteGuard` phía React kiểm tra theo permission (không phải role tĩnh) — đọc danh sách permission trả về trong response `login`/`refresh`, chặn UI không đúng quyền — **đây chỉ là UX**, không thay thế check permission ở backend; mọi endpoint vẫn tự chặn đúng ở server vì client có thể bị bypass.
- Đổi permission của 1 role có hiệu lực với client **tối đa sau ~15 phút** (chu kỳ access token refresh) — không có cơ chế đẩy (push) thông báo đổi quyền tức thời tới client đang mở phiên.
- `GET api/v1/permissions` (catalog toàn bộ Resource+Action hợp lệ), `GET api/v1/role-permissions` (ma trận hiện tại), `POST`/`DELETE api/v1/roles/{role}/permissions/{permissionId}` (cấp/thu hồi) — toàn bộ nhóm này hardcode `Admin` (break-glass).

## 9. Pagination/filter/sort

**[Chưa implement ở backend]** — hiện tại `GetAll` trả về toàn bộ danh sách (không phân trang), chấp nhận được vì Line/Stage/WorkStation là danh mục nhỏ. Khi danh sách lớn dần (User, ProductionPlan, và đặc biệt lịch sử scan sau này), áp dụng quy ước sau — cần làm ở backend trước, `web-admin` không tự bịa tham số khi endpoint chưa hỗ trợ:

- Query string: `?page=1&pageSize=20&sortBy=name&sortDir=asc&search=...`
- `page` bắt đầu từ `1`; `sortDir` chỉ nhận `asc`/`desc`.
- Response bọc trong envelope thay vì mảng trần:
  ```json
  { "items": [ ... ], "totalCount": 137, "page": 1, "pageSize": 20 }
  ```
- Endpoint nào đã hỗ trợ, ghi rõ trong XML doc của Controller action đó — `web-admin` chỉ dùng phân trang khi biết chắc endpoint đã trả đúng envelope trên.

## 10. DateTime/Enum convention

**DateTime**
- Lưu và truyền **UTC**. Field kiểu thời điểm cụ thể đặt hậu tố `Utc` trong tên (tiền lệ đã có: `LoginResponse.ExpiresAtUtc`) để không nhầm với giờ địa phương.
- `System.Text.Json` serialize `DateTime` theo ISO 8601 mặc định (vd. `"2026-08-13T02:00:00Z"`) — không cần converter riêng.
- `web-admin` chỉ format hiển thị theo giờ Việt Nam (UTC+7) ở tầng UI (lúc render), không lưu lại giá trị đã quy đổi giờ local vào state/type.
- Field chỉ mang ý nghĩa **ngày** (không có giờ, vd. `ProductionPlanDto.EffectiveDate`) vẫn đang khai báo kiểu `DateTime` — khi hiển thị/nhập ở `web-admin`, chỉ lấy phần ngày, không hiển thị giờ để tránh gây hiểu nhầm có giờ cụ thể.

**Enum**
- Serialize dạng **chuỗi** (tên enum, vd. `"Admin"`), không dạng số — cấu hình `JsonStringEnumConverter` đã thêm vào `Program.cs` (`AddControllers().AddJsonOptions(...)`) khi viết tài liệu này. Lý do: số không có ý nghĩa với người đọc payload, và dễ vỡ hợp đồng nếu sau này chèn thêm giá trị enum ở giữa làm lệch số thứ tự.
- Áp dụng cho mọi enum hiện có (`UserRole`) và enum thêm sau này — không cần khai báo `[JsonConverter]` riêng lẻ từng property, cấu hình toàn cục đã áp dụng chung.
- Request body gửi enum lên cũng phải gửi dạng chuỗi khớp đúng tên (case-sensitive theo mặc định của `JsonStringEnumConverter`, vd. `"Admin"` chứ không phải `"admin"`).
