# CLAUDE.md — web-admin

Hướng dẫn khi làm việc trong `web-admin/` — ứng dụng React quản lý danh mục cho Admin/Tổ trưởng/Ban quản lý. Xem `Documents/ADR-002-lua-chon-react-cho-web-admin.md` để biết lý do chọn stack, `Documents/ADR-003-httponly-cookie-refresh-token.md` để biết cơ chế auth (HttpOnly Cookie + Refresh Token, đổi vì hệ thống sẽ mở ra public internet), và **`Documents/API-Conventions.md`** để biết hợp đồng API (route, status code, format lỗi, JWT, DateTime/Enum...) — đọc trước khi viết bất kỳ module nào trong `api/`. File `CLAUDE.md` ở gốc repo vẫn áp dụng cho phần business rule chung (SRS, backlog); file này chỉ bổ sung quy ước kỹ thuật riêng cho project React.

## Stack

Vite + React 18 + TypeScript + Ant Design + React Router + TanStack Query (React Query) + Axios.

## Commands

```bash
npm install
npm run dev        # dev server
npm run build       # build production
npm run lint         # ESLint
```

Project này **không** nằm trong `ProductionMES.sln`, không dùng `dotnet build`/`dotnet test`.

## Cấu trúc thư mục

```
web-admin/
  src/
    api/            HTTP client (Axios instance, interceptor JWT) + 1 module gọi API cho mỗi resource
                     (linesApi.ts, stagesApi.ts, workStationsApi.ts, productionPlansApi.ts, usersApi.ts...)
    features/        1 thư mục con cho mỗi domain resource (lines/, stages/, work-stations/, production-plans/, users/)
                     — mỗi feature gồm: page (màn hình danh sách/form), hook TanStack Query riêng (useLines.ts...), component con dùng riêng cho feature đó
    components/      Component dùng chung nhiều feature (layout, route guard, table wrapper...)
    routes/           Khai báo React Router, route-based code splitting (React.lazy) theo feature
    store/            State chỉ tồn tại phía client (auth session, UI toggle) — KHÔNG dùng để cache dữ liệu từ server
    types/            Type khớp với DTO của Application layer (backend) — đặt tên type trùng tên DTO backend để dễ đối chiếu
```

## Nguyên tắc gọi API

- Mọi lời gọi server đi qua TanStack Query (`useQuery`/`useMutation`), không tự quản lý loading/error state thủ công trong component.
- Không cache dữ liệu server vào `store/` (Zustand/Context) — đó là việc của TanStack Query cache. `store/` chỉ chứa state thuần client (session hiện tại, cờ UI).
- Type request/response trong `types/` phải khớp tên field với DTO backend (`ProductionMES.Application/DTOs/`) — khi backend đổi DTO, cập nhật type ở đây theo, không tự suy đoán field.
- Không gọi trực tiếp `fetch`/`axios` rải rác trong component — luôn qua module tương ứng ở `api/`.

## Đặt tên

- Component/type: `PascalCase`, tên file `.tsx` trùng tên component (vd. `LineListPage.tsx` chứa `LineListPage`).
- Biến/hàm/hook: `camelCase`; custom hook bắt đầu bằng `use` (vd. `useLines`, `useCreateLine`).
- Text hiển thị cho người dùng (label, thông báo, tiêu đề) giữ **tiếng Việt** (khớp SRS/nghiệp vụ); định danh trong code (biến, hàm, component, type) dùng **tiếng Anh** — nhất quán quy ước đã chốt ở `CLAUDE.md` gốc cho phần backend.

## Auth

Chi tiết đầy đủ (format request/response, thứ tự gọi, xử lý 401, claim role...) ở `Documents/API-Conventions.md` mục 7-8 và `Documents/ADR-003-httponly-cookie-refresh-token.md`. Điểm bắt buộc phải nhớ khi code phần auth ở `web-admin`:

- Token nằm trong cookie `HttpOnly` — **không** đọc/lưu token bằng JS, **không** dùng `jwt-decode` để lấy thông tin từ token. Thông tin user (username/fullName/userRole) lấy từ body response của `login`/`refresh`, lưu ở `store/` (chỉ trong bộ nhớ, mất khi reload — chấp nhận được vì cookie vẫn còn, gọi lại `refresh` hoặc 1 API `/auth/me` khi app khởi động để phục hồi session).
- Axios instance ở `api/` phải cấu hình `withCredentials: true` để cookie tự gửi kèm.
- Gọi `GET api/v1/auth/csrf` **trước** request đổi dữ liệu đầu tiên (vd. lúc app khởi động), lưu `csrfToken` trả về, gắn vào header `X-CSRF-TOKEN` cho mọi `POST`/`PUT` (interceptor request của Axios, không gắn thủ công từng lời gọi).
- Interceptor response: khi gặp `401` (không phải từ chính `/auth/login`/`/auth/refresh`) → gọi `POST /auth/refresh` đúng 1 lần → nếu thành công, thử lại request gốc; nếu refresh cũng `401` → xóa session ở `store/`, điều hướng login. Tuyệt đối tránh vòng lặp refresh vô hạn khi backend lỗi khác gây 401 dai dẳng.
- Route guard chặn theo role (`Supervisor`/`Admin`/`Manager`, không phục vụ `Operator`) — chỉ là UX, backend luôn là nguồn chặn quyền cuối cùng.

## Form & validate

- Dùng React Hook Form + Zod cho validate phía client.
- Rule validate phải đối chiếu thủ công với FluentValidation validator tương ứng ở backend (`ProductionMES.Application/Validators/`) — không có cơ chế đồng bộ tự động giữa 2 tầng, xem rủi ro đã ghi ở ADR-002.
