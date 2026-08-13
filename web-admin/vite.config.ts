import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import mkcert from 'vite-plugin-mkcert'

// https://vite.dev/config/
// mkcert(): API chạy https://localhost:7230 (dev cert của dotnet), nếu web-admin dev chạy http thì bị coi
// là khác scheme ("schemeful same-site") — cookie SameSite=Strict (ADR-003: access_token/refresh_token/
// XSRF-TOKEN) không được trình duyệt gửi kèm khi gọi API, POST /auth/login luôn bị 400 "CSRF token không
// hợp lệ". mkcert tự tạo + trust 1 local CA cho localhost, ép Vite dev server chạy https để cùng scheme
// với API — không cần cert trùng nhau, không phải nới lỏng SameSite.
export default defineConfig({
  plugins: [react(), mkcert()],
})
