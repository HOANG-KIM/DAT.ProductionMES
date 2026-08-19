import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { getCsrf, refresh } from './api/authApi';
import { useAuthStore } from './store/authStore';
import './index.css';
import { AppRoutes } from './routes/AppRoutes';

const queryClient = new QueryClient();

function renderApp() {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <ConfigProvider locale={viVN}>
          <BrowserRouter>
            <AppRoutes />
          </BrowserRouter>
        </ConfigProvider>
      </QueryClientProvider>
    </StrictMode>,
  );
}

// `authStore` chỉ giữ session trong bộ nhớ, mất mỗi khi reload/F5 (web-admin/CLAUDE.md mục Auth) — cookie
// refresh_token (HttpOnly) vẫn còn nên phục hồi lại session bằng POST /auth/refresh trước khi render, tránh
// RouteGuard thấy `user === null` rồi redirect /login oan dù phiên vẫn hợp lệ. Không có refresh_token hợp lệ
// (chưa đăng nhập hoặc phiên đã hết hạn) → refresh() trả 401, coi là trạng thái bình thường, không log lỗi.
async function restoreSession(): Promise<void> {
  try {
    const response = await refresh();
    useAuthStore.getState().setUser({
      username: response.username,
      fullName: response.fullName,
      userRole: response.userRole,
      permissions: response.permissions,
    });
  } catch {
    // Chưa đăng nhập hoặc phiên hết hạn — giữ nguyên trạng thái logged-out, RouteGuard sẽ redirect /login.
  }
}

// Gọi GET /auth/csrf trước request đổi dữ liệu đầu tiên (API-Conventions.md mục 7). POST /auth/refresh bắt
// buộc CSRF token hợp lệ (AntiforgeryActionFilter) nên phải đợi getCsrf() xong mới gọi restoreSession(). Không
// chặn render vĩnh viễn nếu getCsrf() lỗi (vd. backend chưa chạy) — vẫn hiển thị app (ở trạng thái logged-out).
getCsrf()
  .then(() => restoreSession())
  .catch((error: unknown) => {
    console.error('Không lấy được CSRF token khi khởi động app', error);
  })
  .finally(renderApp);
