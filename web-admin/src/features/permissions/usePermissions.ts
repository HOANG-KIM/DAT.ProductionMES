import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCatalog, getRolePermissionMatrix, grantPermission, revokePermission } from '../../api/permissionsApi';
import type { UserRole } from '../../types/auth';

/** `GET /api/v1/permissions` — catalog toàn bộ permission hợp lệ, dùng dựng cột bảng ma trận. */
export function usePermissionCatalog() {
  return useQuery({
    queryKey: ['permissions'],
    queryFn: getCatalog,
  });
}

/** `GET /api/v1/role-permissions` — ma trận Role × Permission hiện tại. */
export function useRolePermissionMatrix() {
  return useQuery({
    queryKey: ['role-permissions'],
    queryFn: getRolePermissionMatrix,
  });
}

/** `POST /api/v1/roles/{role}/permissions/{permissionId}` — cấp 1 permission cho 1 role. */
export function useGrantPermission() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ role, permissionId }: { role: UserRole; permissionId: number }) => grantPermission(role, permissionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['role-permissions'] });
    },
  });
}

/** `DELETE /api/v1/roles/{role}/permissions/{permissionId}` — thu hồi 1 permission khỏi 1 role. */
export function useRevokePermission() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ role, permissionId }: { role: UserRole; permissionId: number }) => revokePermission(role, permissionId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: ['role-permissions'] });
    },
  });
}
