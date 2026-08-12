import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConfigProvider } from 'antd';
import viVN from 'antd/locale/vi_VN';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { getCsrf } from './api/authApi';
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

// Gọi GET /auth/csrf trước request đổi dữ liệu đầu tiên (API-Conventions.md mục 7). Không chặn
// render vĩnh viễn nếu lỗi (vd. backend chưa chạy) — vẫn hiển thị app, chỉ POST/PUT đầu tiên
// (thường là login) sẽ thiếu CSRF token và bị 400 cho tới khi gọi lại thành công.
getCsrf()
  .catch((error: unknown) => {
    console.error('Không lấy được CSRF token khi khởi động app', error);
  })
  .finally(renderApp);
