import type { UserRole } from './auth';

/**
 * Type khớp DTO User backend (`ProductionMES.Application/DTOs/Users/`, US-22/FR-22).
 * `UsersController` break-glass hardcode `[Authorize(Roles = "Admin")]`, không đi qua permission
 * động (ADR-004) — xem `web-admin/CLAUDE.md`/`Documents/ADR-004-role-permission-dong.md`.
 */

/** Khớp `UserDto`. */
export interface User {
  id: number;
  username: string;
  fullName: string;
  userRole: UserRole;
  isActive: boolean;
}

/** Khớp `CreateUserRequest`. */
export interface CreateUserRequest {
  username: string;
  password: string;
  fullName: string;
  userRole: UserRole;
}

/** Khớp `UpdateUserRoleRequest` — chỉ sửa được vai trò qua API hiện có, không sửa Username/FullName/Password. */
export interface UpdateUserRoleRequest {
  userRole: UserRole;
}
