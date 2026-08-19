import { Route, Routes } from 'react-router-dom';
import { AppLayout } from '../components/AppLayout';
import { PermissionGuard } from '../components/PermissionGuard';
import { RouteGuard } from '../components/RouteGuard';
import { LoginPage } from '../features/auth/LoginPage';
import { HomePage } from '../features/home/HomePage';
import { LineListPage } from '../features/lines/LineListPage';
import { PermissionManagementPage } from '../features/permissions/PermissionManagementPage';
import { ProductionReportPage } from '../features/reports/ProductionReportPage';
import { StageListPage } from '../features/stages/StageListPage';
import { UserListPage } from '../features/users/UserListPage';
import { WorkStationListPage } from '../features/work-stations/WorkStationListPage';

/**
 * Khai báo route toàn app: `/login` public, phần còn lại bọc `RouteGuard` (chặn theo session/role)
 * + `AppLayout` (khung Ant Design). Route `/permissions` và `/users` bọc `PermissionGuard
 * role="Admin"` (break-glass, ADR-004 — không đi qua permission động, `UsersController` cũng hardcode
 * `[Authorize(Roles="Admin")]`). Route `/lines`, `/stages`, `/work-stations`, `/reports/production-summary`
 * (US-21) bọc `PermissionGuard permission="Xxx.View"` (permission động chuẩn ADR-004). Trang CRUD
 * ProductionPlan thuộc phạm vi task sau (không nằm ở web-admin, xem ADR-002).
 */
export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route element={<RouteGuard />}>
        <Route element={<AppLayout />}>
          <Route path="/" element={<HomePage />} />
          <Route
            path="/lines"
            element={
              <PermissionGuard permission="Line.View">
                <LineListPage />
              </PermissionGuard>
            }
          />
          <Route
            path="/stages"
            element={
              <PermissionGuard permission="Stage.View">
                <StageListPage />
              </PermissionGuard>
            }
          />
          <Route
            path="/work-stations"
            element={
              <PermissionGuard permission="WorkStation.View">
                <WorkStationListPage />
              </PermissionGuard>
            }
          />
          <Route
            path="/reports/production-summary"
            element={
              <PermissionGuard permission="Report.View">
                <ProductionReportPage />
              </PermissionGuard>
            }
          />
          <Route
            path="/users"
            element={
              <PermissionGuard role="Admin">
                <UserListPage />
              </PermissionGuard>
            }
          />
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
