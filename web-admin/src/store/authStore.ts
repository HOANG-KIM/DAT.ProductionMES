import { create } from 'zustand';
import type { AuthUser } from '../types/auth';

/**
 * State auth phía client — chỉ tồn tại trong bộ nhớ (mất khi reload trang), KHÔNG lưu token
 * (token nằm trong cookie HttpOnly, JS không đọc được — xem `web-admin/CLAUDE.md` mục Auth).
 * KHÔNG dùng store này để cache dữ liệu server — đó là việc của TanStack Query.
 */
interface AuthState {
  user: AuthUser | null;
  setUser: (user: AuthUser) => void;
  clear: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  setUser: (user) => set({ user }),
  clear: () => set({ user: null }),
}));
