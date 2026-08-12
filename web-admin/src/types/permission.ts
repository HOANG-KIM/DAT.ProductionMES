/**
 * Type khớp DTO Permission backend (`ProductionMES.Application/DTOs/Permissions/`) và enum
 * `ProductionMES.Domain/Enums/{PermissionResource,PermissionAction}.cs` (ADR-004).
 */
import type { UserRole } from './auth';

/** Khớp enum `PermissionResource` backend, serialize dạng chuỗi (mục 10 API-Conventions.md). */
export type PermissionResource = 'Line' | 'Stage' | 'WorkStation' | 'ProductionPlan' | 'ProductionPlanStage';

/** Khớp enum `PermissionAction` backend, serialize dạng chuỗi (mục 10 API-Conventions.md). */
export type PermissionAction = 'View' | 'Create' | 'Update' | 'Activate' | 'Deactivate' | 'Delete';

/** Khớp `PermissionDto` (`ProductionMES.Application/DTOs/Permissions/PermissionDto.cs`). */
export interface Permission {
  id: number;
  resource: PermissionResource;
  action: PermissionAction;
}

/**
 * Khớp `RolePermissionMatrixDto` (`ProductionMES.Application/DTOs/Permissions/RolePermissionMatrixDto.cs`)
 * — 1 dòng cho mỗi `UserRole`, kèm danh sách `id` permission (khớp `Permission.id`) đang được cấp.
 */
export interface RolePermissionMatrixEntry {
  role: UserRole;
  permissionIds: number[];
}
