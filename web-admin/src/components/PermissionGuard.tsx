import type { ReactNode } from 'react';
import { useAuthStore } from '../store/authStore';
import type { UserRole } from '../types/auth';
import { ForbiddenPage } from './ForbiddenPage';

interface PermissionGuardProps {
  children: ReactNode;
  /** Permission động (ADR-004) cần có để xem `children`, dạng `"Resource.Action"` (vd. `"Line.View"`). */
  permission?: string;
  /**
   * Role tĩnh cần có để xem `children` — dùng cho trang break-glass (không đi qua permission động,
   * xem ADR-004), hiện chỉ có màn hình quản lý permission cần mode này.
   */
  role?: UserRole;
}

/**
 * Bọc bên trong `RouteGuard` cho từng route/feature cụ thể — kiểm tra permission động hoặc role
 * tĩnh (dùng 1 trong 2 prop `permission`/`role`). Không đủ điều kiện → render `ForbiddenPage` (403),
 * KHÔNG redirect `/login` (khác `RouteGuard`, vì user đã đăng nhập hợp lệ, chỉ thiếu quyền cho trang này).
 */
export function PermissionGuard({ children, permission, role }: PermissionGuardProps) {
  const user = useAuthStore((state) => state.user);
  const hasPermission = useAuthStore((state) => state.hasPermission);

  const isAllowed = role !== undefined ? user?.userRole === role : permission !== undefined ? hasPermission(permission) : false;

  if (!isAllowed) {
    return <ForbiddenPage />;
  }

  return <>{children}</>;
}
