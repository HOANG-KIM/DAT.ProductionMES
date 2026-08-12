import { Navigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

/**
 * Chặn route theo session hiện có ở `authStore` — chỉ là UX, backend luôn là nguồn chặn quyền
 * cuối cùng (`web-admin/CLAUDE.md` mục Auth). Dùng làm layout route (bọc `Outlet`) trong
 * `routes/AppRoutes.tsx`.
 * - Chưa đăng nhập → redirect `/login`.
 * - Đã đăng nhập nhưng là `Operator` → web-admin không phục vụ role này (vai trò đó thao tác ở
 *   Station.Wpf), chặn tương tự chưa đăng nhập.
 */
export function RouteGuard() {
  const user = useAuthStore((state) => state.user);

  if (!user || user.userRole === 'Operator') {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
