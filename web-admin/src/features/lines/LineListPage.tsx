import { PlusOutlined } from '@ant-design/icons';
import { Alert, Button, message, Modal, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { LineFormModal } from './LineFormModal';
import { useDeactivateLine, useLines } from './useLines';
import { useAuthStore } from '../../store/authStore';
import type { Line } from '../../types/line';

/** Màn hình danh mục Line (US-01) — bảng + modal tạo/sửa, vô hiệu hóa qua `Modal.confirm`. */
export function LineListPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const linesQuery = useLines();
  const deactivateMutation = useDeactivateLine();

  const [formOpen, setFormOpen] = useState(false);
  const [editingLine, setEditingLine] = useState<Line | null>(null);

  const openCreateForm = () => {
    setEditingLine(null);
    setFormOpen(true);
  };

  const openEditForm = (line: Line) => {
    setEditingLine(line);
    setFormOpen(true);
  };

  const handleDeactivate = (line: Line) => {
    Modal.confirm({
      title: `Vô hiệu hóa Line "${line.name}"?`,
      content: 'Line bị vô hiệu hóa sẽ không dùng được cho cấu hình mới, dữ liệu lịch sử vẫn được giữ nguyên.',
      okText: 'Vô hiệu hóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await deactivateMutation.mutateAsync(line.id);
          void message.success(`Đã vô hiệu hóa Line "${line.name}"`);
        } catch {
          void message.error('Vô hiệu hóa Line thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const columns: ColumnsType<Line> = [
    { title: 'Tên Line', dataIndex: 'name', key: 'name' },
    { title: 'Mô tả', dataIndex: 'description', key: 'description' },
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
      render: (_: unknown, line: Line) => (
        <div style={{ display: 'flex', gap: 8 }}>
          {hasPermission('Line.Update') && (
            <Button size="small" onClick={() => openEditForm(line)}>
              Sửa
            </Button>
          )}
          {hasPermission('Line.Deactivate') && line.isActive && (
            <Button size="small" danger onClick={() => handleDeactivate(line)}>
              Vô hiệu hóa
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (linesQuery.isLoading) {
    return <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />;
  }

  if (linesQuery.isError) {
    return <Alert type="error" showIcon message="Không tải được danh sách Line" description="Vui lòng thử tải lại trang." />;
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Quản lý Line
        </Typography.Title>
        {hasPermission('Line.Create') && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateForm}>
            Thêm Line
          </Button>
        )}
      </div>
      <Table<Line> rowKey="id" columns={columns} dataSource={linesQuery.data ?? []} />
      <LineFormModal open={formOpen} editingLine={editingLine} onClose={() => setFormOpen(false)} />
    </div>
  );
}
