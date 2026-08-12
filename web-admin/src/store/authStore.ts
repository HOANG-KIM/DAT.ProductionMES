import { create } from 'zustand';
import type { AuthUser } from '../types/auth';

/**
 * State auth phía client — chỉ tồn tại trong bộ nhớ (mất khi reload trang), KHÔNG lưu token
 * (token nằm trong cookie HttpOnly, JS không đọc được — xem `web-admin/CLAUDE.md` mục Auth).
 * KHÔNG dùng store này để cache dữ liệu server — đó là việc của TanStack Query.
 */
interface AuthState {
  user: AuthUser | null;
  /** Permission hiệu lực (ADR-004) của `user.userRole`, dạng `"Resource.Action"`. Mặc định rỗng khi chưa đăng nhập. */
  permissions: string[];
  setUser: (user: AuthUser) => void;
  clear: () => void;
  /** Kiểm tra `user` hiện tại có permission `"Resource.Action"` truyền vào hay không. */
  hasPermission: (permission: string) => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  permissions: [],
  setUser: (user) => set({ user, permissions: user.permissions }),
  clear: () => set({ user: null, permissions: [] }),
  hasPermission: (permission) => get().permissions.includes(permission),
}));
