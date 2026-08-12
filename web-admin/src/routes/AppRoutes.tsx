import { Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/AppLayout';
import { PermissionGuard } from '../components/PermissionGuard';
import { RouteGuard } from '../components/RouteGuard';
import { LoginPage } from '../features/auth/LoginPage';
import { HomePage } from '../features/home/HomePage';
import { PermissionManagementPage } from '../features/permissions/PermissionManagementPage';

/**
 * Khai báo route toàn app: `/login` public, phần còn lại bọc `RouteGuard` (chặn theo session/role)
 * + `AppLayout` (khung Ant Design). Route `/permissions` bọc thêm `PermissionGuard role="Admin"`
 * (break-glass, ADR-004 — không đi qua permission động). Các trang CRUD
 * Line/Stage/WorkStation/ProductionPlan/User thuộc phạm vi task sau.
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RouteGuard />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route
            path="/permissions"
            element={
              <PermissionGuard role="Admin">
                <PermissionManagementPage />
              </PermissionGuard>
            }
          />
        </Route>
      </Route>
    </Routes>
  );
}
