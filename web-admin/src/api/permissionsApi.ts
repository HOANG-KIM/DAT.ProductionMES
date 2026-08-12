import { httpClient } from './httpClient';
import type { UserRole } from '../types/auth';
import type { Permission, RolePermissionMatrixEntry } from '../types/permission';

/**
 * Gọi API quản lý Permission/RolePermission (ADR-004) — break-glass, hardcode `Admin` ở backend
 * (`PermissionsController`). Route không dùng chung 1 tiền tố resource duy nhất, khớp đúng route
 * thật của Controller (`permissions`, `role-permissions`, `roles/{role}/permissions/{permissionId}`).
 */

/** `GET /api/v1/permissions` — catalog toàn bộ Permission hợp lệ. */
export async function getCatalog(): Promise<Permission[]> {
  const response = await httpClient.get<Permission[]>('/api/v1/permissions');
  return response.data;
}

/** `GET /api/v1/role-permissions` — ma trận Role × Permission hiện tại. */
export async function getRolePermissionMatrix(): Promise<RolePermissionMatrixEntry[]> {
  const response = await httpClient.get<RolePermissionMatrixEntry[]>('/api/v1/role-permissions');
  return response.data;
}

/** `POST /api/v1/roles/{role}/permissions/{permissionId}` — cấp 1 permission cho 1 role. Trả `204 No Content`. */
export async function grantPermission(role: UserRole, permissionId: number): Promise<void> {
  await httpClient.post(`/api/v1/roles/${role}/permissions/${permissionId}`);
}

/** `DELETE /api/v1/roles/{role}/permissions/{permissionId}` — thu hồi 1 permission khỏi 1 role. Trả `204 No Content`. */
export async function revokePermission(role: UserRole, permissionId: number): Promise<void> {
  await httpClient.delete(`/api/v1/roles/${role}/permissions/${permissionId}`);
}
