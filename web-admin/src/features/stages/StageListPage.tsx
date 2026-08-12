import { PlusOutlined } from '@ant-design/icons';
import { Alert, Button, message, Modal, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { StageFormModal } from './StageFormModal';
import { useDeactivateStage, useStages } from './useStages';
import { useAuthStore } from '../../store/authStore';
import type { Stage } from '../../types/stage';

/** Màn hình danh mục Stage (US-02) — bảng + modal tạo/sửa, vô hiệu hóa qua `Modal.confirm`. */
export function StageListPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const stagesQuery = useStages();
  const deactivateMutation = useDeactivateStage();

  const [formOpen, setFormOpen] = useState(false);
  const [editingStage, setEditingStage] = useState<Stage | null>(null);

  const openCreateForm = () => {
    setEditingStage(null);
    setFormOpen(true);
  };

  const openEditForm = (stage: Stage) => {
    setEditingStage(stage);
    setFormOpen(true);
  };

  const handleDeactivate = (stage: Stage) => {
    Modal.confirm({
      title: `Vô hiệu hóa công đoạn "${stage.name}"?`,
      content: 'Công đoạn bị vô hiệu hóa sẽ không dùng được cho cấu hình mới, dữ liệu lịch sử vẫn được giữ nguyên.',
      okText: 'Vô hiệu hóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await deactivateMutation.mutateAsync(stage.id);
          void message.success(`Đã vô hiệu hóa công đoạn "${stage.name}"`);
        } catch {
          void message.error('Vô hiệu hóa công đoạn thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const columns: ColumnsType<Stage> = [
    { title: 'Tên công đoạn', dataIndex: 'name', key: 'name' },
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
      render: (_: unknown, stage: Stage) => (
        <div style={{ display: 'flex', gap: 8 }}>
          {hasPermission('Stage.Update') && (
            <Button size="small" onClick={() => openEditForm(stage)}>
              Sửa
            </Button>
          )}
          {hasPermission('Stage.Deactivate') && stage.isActive && (
            <Button size="small" danger onClick={() => handleDeactivate(stage)}>
              Vô hiệu hóa
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (stagesQuery.isLoading) {
    return <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />;
  }

  if (stagesQuery.isError) {
    return (
      <Alert type="error" showIcon message="Không tải được danh sách công đoạn" description="Vui lòng thử tải lại trang." />
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Quản lý Công đoạn
        </Typography.Title>
        {hasPermission('Stage.Create') && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateForm}>
            Thêm công đoạn
          </Button>
        )}
      </div>
      <Table<Stage> rowKey="id" columns={columns} dataSource={stagesQuery.data ?? []} />
      <StageFormModal open={formOpen} editingStage={editingStage} onClose={() => setFormOpen(false)} />
    </div>
  );
}
