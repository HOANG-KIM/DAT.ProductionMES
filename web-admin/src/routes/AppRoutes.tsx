import { Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/AppLayout';
import { RouteGuard } from '../components/RouteGuard';
import { LoginPage } from '../features/auth/LoginPage';
import { HomePage } from '../features/home/HomePage';

/**
 * Khai báo route toàn app: `/login` public, phần còn lại bọc `RouteGuard` (chặn theo session/role)
 * + `AppLayout` (khung Ant Design). Route con hiện chỉ có 1 trang placeholder "Trang chủ" — các
 * trang CRUD Line/Stage/WorkStation/ProductionPlan/User thuộc phạm vi task sau.
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RouteGuard />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
        </Route>
      </Route>
    </Routes>
  );
}
