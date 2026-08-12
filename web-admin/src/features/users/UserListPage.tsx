import { PlusOutlined } from '@ant-design/icons';
import { Alert, Button, message, Modal, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { ROLE_LABELS } from './roleLabels';
import { UserCreateFormModal } from './UserCreateFormModal';
import { UserRoleFormModal } from './UserRoleFormModal';
import { useDeactivateUser, useUsers } from './useUsers';
import type { User } from '../../types/user';

/**
 * Màn hình quản lý người dùng (US-22) — bảng + modal tạo mới (đủ field)/sửa vai trò (chỉ 1 field),
 * vô hiệu hóa qua `Modal.confirm`. `UsersController` break-glass hardcode Admin (ADR-004), route/menu
 * bọc `PermissionGuard role="Admin"` ở `AppRoutes.tsx` — không dùng `hasPermission` permission động.
 */
export function UserListPage() {
  const usersQuery = useUsers();
  const deactivateMutation = useDeactivateUser();

  const [createFormOpen, setCreateFormOpen] = useState(false);
  const [editingUser, setEditingUser] = useState<User | null>(null);

  const openRoleForm = (user: User) => {
    setEditingUser(user);
  };

  const handleDeactivate = (user: User) => {
    Modal.confirm({
      title: `Vô hiệu hóa người dùng "${user.username}"?`,
      content: 'Tài khoản bị vô hiệu hóa sẽ không đăng nhập được, dữ liệu lịch sử vẫn được giữ nguyên.',
      okText: 'Vô hiệu hóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await deactivateMutation.mutateAsync(user.id);
          void message.success(`Đã vô hiệu hóa người dùng "${user.username}"`);
        } catch {
          void message.error('Vô hiệu hóa người dùng thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const columns: ColumnsType<User> = [
    { title: 'Tên đăng nhập', dataIndex: 'username', key: 'username' },
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    {
      title: 'Vai trò',
      dataIndex: 'userRole',
      key: 'userRole',
      render: (userRole: User['userRole']) => ROLE_LABELS[userRole],
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      key: 'isActive',
      render: (isActive: boolean) => (
        <Tag color={isActive ? 'green' : 'default'}>{isActive ? 'Đang hoạt động' : 'Đã vô hiệu hóa'}</Tag>
      ),
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, user: User) => (
        <div style={{ display: 'flex', gap: 8 }}>
          <Button size="small" onClick={() => openRoleForm(user)}>
            Sửa vai trò
          </Button>
          {user.isActive && (
            <Button size="small" danger onClick={() => handleDeactivate(user)}>
              Vô hiệu hóa
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (usersQuery.isLoading) {
    return <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />;
  }

  if (usersQuery.isError) {
    return (
      <Alert type="error" showIcon message="Không tải được danh sách người dùng" description="Vui lòng thử tải lại trang." />
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Quản lý người dùng
        </Typography.Title>
        <Button type="primary" icon={<PlusOutlined />} onClick={() => setCreateFormOpen(true)}>
          Thêm người dùng
        </Button>
      </div>
      <Table<User> rowKey="id" columns={columns} dataSource={usersQuery.data ?? []} />
      <UserCreateFormModal open={createFormOpen} onClose={() => setCreateFormOpen(false)} />
      <UserRoleFormModal open={editingUser !== null} editingUser={editingUser} onClose={() => setEditingUser(null)} />
    </div>
  );
}
