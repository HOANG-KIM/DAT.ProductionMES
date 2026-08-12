import type { UserRole } from '../../types/auth';

/**
 * Label tiếng Việt cho 4 role hệ thống — dùng chung cho dropdown chọn vai trò và hiển thị bảng ở
 * feature `users`. Đặt tên tương tự `ROLE_LABELS` ở `features/permissions/PermissionManagementPage.tsx`
 * (không import từ đó vì đó là const cục bộ của page khác, không export).
 */
export const ROLE_LABELS: Record<UserRole, string> = {
  Operator: 'Vận hành viên',
  Supervisor: 'Tổ trưởng',
  Admin: 'Quản trị viên',
  Manager: 'Quản lý',
};

/** Danh sách option cho `Select` chọn vai trò. */
export const ROLE_OPTIONS: Array<{ value: UserRole; label: string }> = (
  Object.keys(ROLE_LABELS) as UserRole[]
).map((role) => ({ value: role, label: ROLE_LABELS[role] }));
