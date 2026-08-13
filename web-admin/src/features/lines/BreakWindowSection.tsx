import { PlusOutlined } from '@ant-design/icons';
import { Alert, Button, Empty, message, Modal, Spin, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { BreakWindowFormModal } from './BreakWindowFormModal';
import { useBreakWindows, useDeleteBreakWindow } from './useBreakWindows';
import type { BreakWindow } from '../../types/breakWindow';

interface BreakWindowSectionProps {
  lineId: number;
}

/**
 * Danh sách khung giờ nghỉ của 1 Line (US-01a) — nhúng trong màn hình sửa Line (`LineFormModal`), vì hệ thống
 * hiện chưa có màn hình "chi tiết Line" riêng. Áp dụng chung cho mọi kế hoạch sản xuất chạy trên Line (AC1),
 * Line không cấu hình khung nào vẫn hoạt động bình thường (AC4 — hiển thị rỗng, không lỗi).
 */
export function BreakWindowSection({ lineId }: BreakWindowSectionProps) {
  const breakWindowsQuery = useBreakWindows(lineId);
  const deleteMutation = useDeleteBreakWindow(lineId);

  const [formOpen, setFormOpen] = useState(false);
  const [editingBreakWindow, setEditingBreakWindow] = useState<BreakWindow | null>(null);

  const openCreateForm = () => {
    setEditingBreakWindow(null);
    setFormOpen(true);
  };

  const openEditForm = (breakWindow: BreakWindow) => {
    setEditingBreakWindow(breakWindow);
    setFormOpen(true);
  };

  const handleDelete = (breakWindow: BreakWindow) => {
    Modal.confirm({
      title: `Xóa khung giờ nghỉ ${breakWindow.startTime.slice(0, 5)}–${breakWindow.endTime.slice(0, 5)}?`,
      content: 'Thay đổi có hiệu lực ngay cho các lần tính sản lượng kế hoạch lũy kế tiếp theo.',
      okText: 'Xóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await deleteMutation.mutateAsync(breakWindow.id);
          void message.success('Đã xóa khung giờ nghỉ');
        } catch {
          void message.error('Xóa khung giờ nghỉ thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const columns: ColumnsType<BreakWindow> = [
    { title: 'Giờ bắt đầu', dataIndex: 'startTime', key: 'startTime', render: (v: string) => v.slice(0, 5) },
    { title: 'Giờ kết thúc', dataIndex: 'endTime', key: 'endTime', render: (v: string) => v.slice(0, 5) },
    { title: 'Ghi chú', dataIndex: 'note', key: 'note' },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, breakWindow: BreakWindow) => (
        <div style={{ display: 'flex', gap: 8 }}>
          <Button size="small" onClick={() => openEditForm(breakWindow)}>
            Sửa
          </Button>
          <Button size="small" danger onClick={() => handleDelete(breakWindow)}>
            Xóa
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div style={{ marginTop: 8 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
        <Typography.Text strong>Khung giờ nghỉ của Line</Typography.Text>
        <Button size="small" icon={<PlusOutlined />} onClick={openCreateForm}>
          Thêm khung giờ nghỉ
        </Button>
      </div>

      {breakWindowsQuery.isLoading && <Spin size="small" />}
      {breakWindowsQuery.isError && (
        <Alert type="error" showIcon message="Không tải được danh sách khung giờ nghỉ" />
      )}
      {breakWindowsQuery.isSuccess && breakWindowsQuery.data.length === 0 && (
        <Empty description="Line chưa cấu hình khung giờ nghỉ nào" image={Empty.PRESENTED_IMAGE_SIMPLE} />
      )}
      {breakWindowsQuery.isSuccess && breakWindowsQuery.data.length > 0 && (
        <Table<BreakWindow>
          size="small"
          rowKey="id"
          pagination={false}
          columns={columns}
          dataSource={breakWindowsQuery.data}
        />
      )}

      <BreakWindowFormModal
        open={formOpen}
        lineId={lineId}
        editingBreakWindow={editingBreakWindow}
        onClose={() => setFormOpen(false)}
      />
    </div>
  );
}
