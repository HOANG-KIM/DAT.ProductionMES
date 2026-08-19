import { Alert, Checkbox, Spin, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useCallback, useMemo } from 'react';
import type { UserRole } from '../../types/auth';
import type { Permission, PermissionAction, PermissionResource } from '../../types/permission';
import { useGrantPermission, usePermissionCatalog, useRevokePermission, useRolePermissionMatrix } from './usePermissions';
import { useDocumentTitle } from '../../hooks/useDocumentTitle';

/** 4 role hệ thống — hàng cố định của ma trận (không lấy từ API, khớp enum `UserRole` backend). */
const ROLES: UserRole[] = ['Operator', 'Supervisor', 'Admin', 'Manager'];

const ROLE_LABELS: Record<UserRole, string> = {
  Operator: 'Vận hành viên',
  Supervisor: 'Tổ trưởng',
  Admin: 'Quản trị viên',
  Manager: 'Quản lý',
};

const RESOURCE_LABELS: Record<PermissionResource, string> = {
  Line: 'Line sản xuất',
  Stage: 'Công đoạn',
  WorkStation: 'Trạm làm việc',
  ProductionPlan: 'Kế hoạch sản xuất',
  ProductionPlanStage: 'Công đoạn kế hoạch',
};

const ACTION_LABELS: Record<PermissionAction, string> = {
  View: 'Xem',
  Create: 'Tạo mới',
  Update: 'Cập nhật',
  Activate: 'Kích hoạt',
  Deactivate: 'Vô hiệu hóa',
  Delete: 'Xóa',
};

interface RoleRow {
  key: UserRole;
  role: UserRole;
}

/**
 * Màn hình quản lý permission (ADR-004) — ma trận Role × Permission, Admin bật/tắt trực tiếp qua
 * checkbox. Cột được group theo `resource` (header nhóm, vd. "Line sản xuất" gồm các cột con
 * View/Create/Update/Deactivate) cho dễ nhìn — dùng `Table` với `column.children` (pattern nested
 * header chuẩn của Ant Design) thay vì tự dựng bảng HTML thô, vì đã có sẵn `antd` trong stack.
 * Không phân trang (chỉ ~21 permission, 4 role — không thuộc phạm vi mục 9 API-Conventions.md).
 */
export function PermissionManagementPage() {
  useDocumentTitle('Quản lý phân quyền');

  const catalogQuery = usePermissionCatalog();
  const matrixQuery = useRolePermissionMatrix();
  const grantMutation = useGrantPermission();
  const revokeMutation = useRevokePermission();

  const permissionsByResource = useMemo(() => {
    const catalog = catalogQuery.data ?? [];
    const map = new Map<PermissionResource, Permission[]>();
    for (const permission of catalog) {
      const list = map.get(permission.resource) ?? [];
      list.push(permission);
      map.set(permission.resource, list);
    }
    return map;
  }, [catalogQuery.data]);

  const grantedPermissionIdsByRole = useMemo(() => {
    const map = new Map<UserRole, Set<number>>();
    for (const entry of matrixQuery.data ?? []) {
      map.set(entry.role, new Set(entry.permissionIds));
    }
    return map;
  }, [matrixQuery.data]);

  const handleToggle = useCallback(
    (role: UserRole, permissionId: number, checked: boolean) => {
      if (checked) {
        grantMutation.mutate({ role, permissionId });
      } else {
        revokeMutation.mutate({ role, permissionId });
      }
    },
    [grantMutation, revokeMutation],
  );

  const columns: ColumnsType<RoleRow> = useMemo(() => {
    const roleColumn: ColumnsType<RoleRow>[number] = {
      title: 'Vai trò',
      dataIndex: 'role',
      key: 'role',
      fixed: 'left',
      render: (role: UserRole) => ROLE_LABELS[role],
    };

    const resourceColumns: ColumnsType<RoleRow> = Array.from(permissionsByResource.entries()).map(
      ([resource, permissions]) => ({
        title: RESOURCE_LABELS[resource],
        key: resource,
        children: permissions.map((permission) => ({
          title: ACTION_LABELS[permission.action],
          key: permission.id,
          align: 'center' as const,
          render: (_: unknown, row: RoleRow) => (
            <Checkbox
              checked={grantedPermissionIdsByRole.get(row.role)?.has(permission.id) ?? false}
              disabled={grantMutation.isPending || revokeMutation.isPending}
              onChange={(e) => handleToggle(row.role, permission.id, e.target.checked)}
            />
          ),
        })),
      }),
    );

    return [roleColumn, ...resourceColumns];
  }, [permissionsByResource, grantedPermissionIdsByRole, grantMutation.isPending, revokeMutation.isPending, handleToggle]);

  const dataSource: RoleRow[] = ROLES.map((role) => ({ key: role, role }));

  if (catalogQuery.isLoading || matrixQuery.isLoading) {
    return <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />;
  }

  if (catalogQuery.isError || matrixQuery.isError) {
    return (
      <Alert
        type="error"
        showIcon
        message="Không tải được dữ liệu permission"
        description="Vui lòng thử tải lại trang."
      />
    );
  }

  return (
    <div>
      <Typography.Title level={3}>Quản lý phân quyền</Typography.Title>
      <Typography.Paragraph>
        Bật/tắt permission cho từng vai trò. Thay đổi có hiệu lực với phiên đang mở của người dùng khác sau tối đa 15 phút.
      </Typography.Paragraph>
      <Table<RoleRow>
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        bordered
        scroll={{ x: 'max-content' }}
      />
    </div>
  );
}
