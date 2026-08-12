import { PlusOutlined } from '@ant-design/icons';
import { Alert, Button, message, Modal, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { WorkStationFormModal } from './WorkStationFormModal';
import { useDeactivateWorkStation, useWorkStations } from './useWorkStations';
import { useLines } from '../lines/useLines';
import { useStages } from '../stages/useStages';
import { useAuthStore } from '../../store/authStore';
import type { WorkStation } from '../../types/workStation';

/** Màn hình danh mục WorkStation (US-04) — bảng + modal tạo/sửa, vô hiệu hóa qua `Modal.confirm`. */
export function WorkStationListPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const workStationsQuery = useWorkStations();
  const linesQuery = useLines();
  const stagesQuery = useStages();
  const deactivateMutation = useDeactivateWorkStation();

  const [formOpen, setFormOpen] = useState(false);
  const [editingWorkStation, setEditingWorkStation] = useState<WorkStation | null>(null);

  const lineNameById = useMemo(() => {
    const map = new Map<number, string>();
    for (const line of linesQuery.data ?? []) {
      map.set(line.id, line.name);
    }
    return map;
  }, [linesQuery.data]);

  const stageNameById = useMemo(() => {
    const map = new Map<number, string>();
    for (const stage of stagesQuery.data ?? []) {
      map.set(stage.id, stage.name);
    }
    return map;
  }, [stagesQuery.data]);

  const openCreateForm = () => {
    setEditingWorkStation(null);
    setFormOpen(true);
  };

  const openEditForm = (workStation: WorkStation) => {
    setEditingWorkStation(workStation);
    setFormOpen(true);
  };

  const handleDeactivate = (workStation: WorkStation) => {
    Modal.confirm({
      title: `Vô hiệu hóa trạm làm việc "${workStation.name}"?`,
      content: 'Trạm bị vô hiệu hóa sẽ không dùng được cho cấu hình mới, dữ liệu lịch sử vẫn được giữ nguyên.',
      okText: 'Vô hiệu hóa',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await deactivateMutation.mutateAsync(workStation.id);
          void message.success(`Đã vô hiệu hóa trạm làm việc "${workStation.name}"`);
        } catch {
          void message.error('Vô hiệu hóa trạm làm việc thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const columns: ColumnsType<WorkStation> = [
    { title: 'Tên trạm', dataIndex: 'name', key: 'name' },
    {
      title: 'Line',
      dataIndex: 'lineId',
      key: 'lineId',
      render: (lineId: number) => lineNameById.get(lineId) ?? lineId,
    },
    {
      title: 'Công đoạn',
      dataIndex: 'stageId',
      key: 'stageId',
      render: (stageId: number) => stageNameById.get(stageId) ?? stageId,
    },
    {
      title: 'Arduino',
      dataIndex: 'useArduino',
      key: 'useArduino',
      render: (useArduino: boolean) => (useArduino ? <Tag color="blue">Có</Tag> : <Tag>Không</Tag>),
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
      render: (_: unknown, workStation: WorkStation) => (
        <div style={{ display: 'flex', gap: 8 }}>
          {hasPermission('WorkStation.Update') && (
            <Button size="small" onClick={() => openEditForm(workStation)}>
              Sửa
            </Button>
          )}
          {hasPermission('WorkStation.Deactivate') && workStation.isActive && (
            <Button size="small" danger onClick={() => handleDeactivate(workStation)}>
              Vô hiệu hóa
            </Button>
          )}
        </div>
      ),
    },
  ];

  if (workStationsQuery.isLoading) {
    return <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />;
  }

  if (workStationsQuery.isError) {
    return (
      <Alert
        type="error"
        showIcon
        message="Không tải được danh sách trạm làm việc"
        description="Vui lòng thử tải lại trang."
      />
    );
  }

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Quản lý Trạm làm việc
        </Typography.Title>
        {hasPermission('WorkStation.Create') && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateForm}>
            Thêm trạm làm việc
          </Button>
        )}
      </div>
      <Table<WorkStation> rowKey="id" columns={columns} dataSource={workStationsQuery.data ?? []} />
      <WorkStationFormModal open={formOpen} editingWorkStation={editingWorkStation} onClose={() => setFormOpen(false)} />
    </div>
  );
}
