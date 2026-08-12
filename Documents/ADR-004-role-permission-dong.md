# ADR-004: Permission theo Role lưu ở DB, quản lý runtime qua Admin UI

## Trạng thái
**Đã chấp thuận** (Accepted)

## Ngày
12/08/2026

## Bối cảnh (Context)

Từ US-01 đến US-06/US-22, phân quyền theo role được hardcode trực tiếp bằng `[Authorize(Roles = "...")]` ở mức Controller — mỗi Controller 1 danh sách role cố định, biên dịch cứng vào code:

| Controller | Role hardcode |
|---|---|
| `LinesController`, `StagesController`, `WorkStationsController`, `UsersController` | `Admin` |
| `ProductionPlansController`, `ProductionPlanStagesController` | `Supervisor,Admin` |

Khi thiết kế `RouteGuard` phía `web-admin` (ADR-002), phát sinh 1 lỗi: `web-admin/CLAUDE.md` và `API-Conventions.md` mô tả sai — nói `web-admin` "phục vụ Supervisor/Admin/Manager" chung chung, không khớp bảng thật ở trên (vd. không controller nào cho phép `Manager`; `Supervisor` chỉ được phép ở 2/6 controller). Khi rà soát lại để sửa lỗi này, xác nhận thêm: action thật của từng resource **không đồng nhất** — `ProductionPlan` có thêm `Activate`; `ProductionPlanStage` dùng `Delete` (HTTP DELETE thật) thay vì `Deactivate` như các resource còn lại.

Thay vì chỉ vá lại `RouteGuard`/tài liệu cho khớp hiện trạng hardcode, quyết định chuyển hẳn sang mô hình **permission lưu ở DB, Admin tự quản lý qua UI runtime** — không cần sửa code/deploy lại mỗi khi cần đổi quyền của 1 role.

## Quyết định (Decision)

1. Permission mô hình hóa theo cặp **(Resource, Action)** — không dùng CRUD chuẩn (`Create/Read/Update/Delete`) rập khuôn, mà seed đúng action thật đang tồn tại ở từng resource (xem mục Ghi chú triển khai).
2. Entity mới `Permission` (catalog toàn bộ cặp Resource+Action hợp lệ) và `RolePermission` (role nào được cấp permission nào — đây là bảng Admin chỉnh qua UI).
3. Authorization chuyển từ `[Authorize(Roles = "...")]` hardcode sang custom policy-based authorization, tra cứu `RolePermission` (qua cache, không query DB mỗi request).
4. **Break-glass**: API quản lý `User` (đã có, `UsersController`) và API quản lý `Permission`/`RolePermission` (mới) **giữ hardcode `[Authorize(Roles = "Admin")]`**, KHÔNG đi qua hệ thống permission động — đảm bảo luôn có đường quản trị hoạt động được kể cả khi Admin cấu hình sai permission của chính role Admin cho các resource khác.
5. `web-admin` thêm màn hình **quản lý permission** (ma trận Role × Permission, Admin bật/tắt), và `RouteGuard` chuyển từ hardcode `allowedRoles` sang kiểm tra permission động (lấy từ response `login`/`refresh`).

## Lý do (Rationale)

1. **Đúng yêu cầu ban đầu của user** — permission cần lưu DB và quản lý được qua UI runtime, không hardcode string ở Controller, không cần deploy lại khi đổi quyền.
2. **Permission theo action thật của từng resource, không ép khuôn CRUD** — vì hệ thống có action lệch chuẩn thật sự (`Activate` ở `ProductionPlan`, `Delete` thay vì `Deactivate` ở `ProductionPlanStage`; không có `Delete` cứng ở các resource còn lại) — nếu ép về 1 bộ action chung sẽ tạo ra permission "ma" (action không ứng với endpoint nào) hoặc thiếu permission cho action có thật.
3. **Break-glass ngăn nguy cơ tự khóa quyền toàn hệ thống** — đây là rủi ro kinh điển của RBAC tự cấu hình: nếu chính API cấp/thu hồi permission cũng bị chi phối bởi permission động, 1 thao tác sai của Admin (tự thu hồi quyền quản lý permission của role Admin) có thể khóa toàn bộ khả năng sửa lại, chỉ còn cách can thiệp thẳng DB.
4. **Cache thay vì query DB mỗi request** — vì permission được kiểm tra ở MỌI request cần xác thực, query DB trực tiếp mỗi lần sẽ ảnh hưởng hiệu năng; cache in-memory + invalidate ngay khi Admin đổi permission là đủ (không cần độ trễ near-zero, hệ thống nội bộ quy mô nhỏ).

## Phương án khác đã xem xét (Alternatives Considered)

**Giữ hardcode `[Authorize(Roles=...)]`, chỉ sửa `RouteGuard`/tài liệu cho khớp**
- *Ưu điểm*: đơn giản nhất, đúng phạm vi lỗi ban đầu (RouteGuard sai).
- *Vì sao không chọn*: user xác nhận rõ mong muốn ban đầu là permission phải lưu DB và quản lý được runtime — đây là yêu cầu nghiệp vụ thật, không phải chi tiết kỹ thuật có thể bỏ qua.

**Ép permission về CRUD chuẩn (Create/Read/Update/Delete) cho mọi resource**
- *Ưu điểm*: đơn giản, dễ hiểu, khớp quy ước phổ biến.
- *Vì sao không chọn*: không khớp thực tế endpoint đã implement (xem mục Bối cảnh) — sẽ tạo permission không tương ứng hành động thật nào, hoặc thiếu permission cho hành động thật có (`Activate`).

**Permission check permission động luôn cho cả API quản lý User/Permission (không có break-glass)**
- *Ưu điểm*: nhất quán tuyệt đối, không có ngoại lệ.
- *Vì sao không chọn*: rủi ro tự khóa quyền toàn hệ thống qua chính UI — hệ quả nghiêm trọng hơn nhiều so với lợi ích "nhất quán tuyệt đối".

## Hệ quả (Consequences)

**Tích cực**
- Đổi quyền của 1 role (vd. cho phép `Manager` xem `ProductionPlan`) chỉ cần thao tác trên UI, không cần sửa code/deploy lại.
- Permission khớp chính xác action thật từng resource, không có permission "ma".
- Có đường quản trị "thoát hiểm" (User/Permission management hardcode Admin) không thể bị khóa qua UI.

**Tiêu cực / Rủi ro cần lưu ý**
- Tăng đáng kể độ phức tạp so với `[Authorize(Roles=...)]` hardcode: thêm 2 entity + migration + seed, custom authorization handler/policy provider, cache + invalidation, Controller mới quản lý permission, màn hình React mới.
- Toàn bộ Controller đang dùng `[Authorize(Roles=...)]` (trừ `UsersController`) phải refactor sang attribute permission mới, đổi từng action riêng lẻ (không còn 1 rule chung ở mức Controller).
- `web-admin` phải đổi mô hình `RouteGuard` từ role tĩnh sang permission động lấy từ response đăng nhập — nếu Admin đổi permission giữa phiên làm việc, client chỉ cập nhật lại khi access token refresh (tối đa ~15 phút trễ, chấp nhận được ở quy mô hệ thống này).
- Cần đảm bảo cache permission được invalidate đúng lúc Admin chỉnh sửa qua UI — nếu quên, permission mới không có hiệu lực ngay dù DB đã đổi.

## Ghi chú triển khai

**Bảng `Permission` — seed đúng action thật hiện có** (không phải tích chéo đầy đủ):
- `Line`, `Stage`, `WorkStation`: `View`, `Create`, `Update`, `Deactivate`
- `ProductionPlan`: `View`, `Create`, `Update`, `Activate`, `Deactivate`
- `ProductionPlanStage`: `View`, `Create`, `Update`, `Delete`
- `User` — KHÔNG đưa vào bảng `Permission` (nằm trong nhóm break-glass, giữ `[Authorize(Roles="Admin")]` như cũ).

**Seed `RolePermission` ban đầu** — khớp đúng hành vi hiện tại (không đổi behavior khi migrate):
- `Admin`: toàn bộ permission ở trên.
- `Supervisor`: toàn bộ permission của `ProductionPlan` + `ProductionPlanStage`.
- `Operator`, `Manager`: không permission nào (khớp thực tế hiện tại — chưa endpoint nào cho 2 role này).

**Authorization**: custom `IAuthorizationRequirement`/`IAuthorizationHandler` (ASP.NET Core policy-based), tra `RolePermission` qua cache in-memory (nạp lúc cần, invalidate ngay khi API quản lý permission ghi thay đổi). Attribute áp lên từng action cụ thể (không còn ở mức Controller), vì action trong cùng Controller có thể khác permission nhau (vd. `ProductionPlan.Activate` khác `ProductionPlan.Update`).

**API quản lý permission (mới, hardcode Admin)**: catalog toàn bộ `Permission`, xem ma trận `RolePermission` hiện tại, cấp/thu hồi permission cho 1 role.

**`web-admin`**: response `login`/`refresh` bổ sung danh sách permission hiệu lực của role hiện tại (dạng `"Resource.Action"`, vd. `"Line.View"`); `RouteGuard` kiểm tra theo permission thay vì role tĩnh; thêm màn hình ma trận Role × Permission cho Admin.

**Sửa kèm 2 lỗi tài liệu phát hiện trong lúc thiết kế** (không liên quan trực tiếp ADR này nhưng phát hiện cùng lúc): `API-Conventions.md` mục 2 từng ghi "không dùng HTTP DELETE" — sai, `ProductionPlanStagesController.Remove` đã dùng `HttpDelete` thật; mục 8 từng ghi sai ma trận quyền hiện có. Cả 2 cần cập nhật lại cho khớp thực tế.
