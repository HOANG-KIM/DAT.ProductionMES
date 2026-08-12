import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import mkcert from 'vite-plugin-mkcert'

// https://vite.dev/config/
// LƯU Ý: đường dẫn project thật chứa ký tự "#" (E:\1. C#\...), Vite/Rolldown hiểu nhầm "#" thành URL
// fragment ở nhiều chỗ khác nhau. 2 lớp xử lý cần cả hai, không lớp nào đủ 1 mình:
// 1. Chạy "npm run dev" qua directory junction "E:\DAT-web-admin" (trỏ tới thư mục này, không chứa "#")
//    thay vì đường dẫn thật — nếu không, Vite không serve được file .tsx lúc dev (lỗi "Pre-transform
//    error... Does the file exist?"). Xem Documents/HUONG-DAN-CHAY-DEV.md.
// 2. optimizeDeps bên dưới — dù đã chạy qua junction, bước dependency scan (pre-bundling) của Rolldown vẫn
//    tự resolve về đường dẫn thật ở tầng khác và fail ("Failed to run dependency scan"), khiến Vite bỏ qua
//    pre-bundling — chấp nhận được (không lỗi chức năng) nhưng làm lần load đầu chậm hơn và có rủi ro với
//    package CJS-only. Liệt kê thẳng dependency cần pre-bundle để né hẳn bước scan (không cần nó tự dò từ
//    index.html/import graph nữa).
//
// mkcert(): API chạy https://localhost:7230 (dev cert của dotnet), nếu web-admin dev chạy http thì bị coi
// là khác scheme ("schemeful same-site") — cookie SameSite=Strict (ADR-003: access_token/refresh_token/
// XSRF-TOKEN) không được trình duyệt gửi kèm khi gọi API, POST /auth/login luôn bị 400 "CSRF token không
// hợp lệ". mkcert tự tạo + trust 1 local CA cho localhost, ép Vite dev server chạy https để cùng scheme
// với API — không cần cert trùng nhau, không phải nới lỏng SameSite.
export default defineConfig({
  plugins: [react(), mkcert()],
  optimizeDeps: {
    noDiscovery: true,
    include: [
      'react',
      'react-dom',
      'react-dom/client',
      'react-router-dom',
      'antd',
      'antd/locale/vi_VN',
      '@ant-design/icons',
      '@tanstack/react-query',
      'axios',
      'zustand',
      'react-hook-form',
      'zod',
      '@hookform/resolvers/zod',
    ],
  },
})
