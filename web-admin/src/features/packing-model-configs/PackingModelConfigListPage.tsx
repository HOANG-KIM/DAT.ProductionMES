import { PlusOutlined } from '@ant-design/icons';
import { Button, message, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { PackingModelConfigFormModal } from './PackingModelConfigFormModal';
import { useDownloadPackingTemplate, usePackingModelConfigs } from './usePackingModelConfigs';
import { useAuthStore } from '../../store/authStore';
import { useDocumentTitle } from '../../hooks/useDocumentTitle';
import type { PackingModelConfig } from '../../types/packingModelConfig';

/**
 * Màn hình cấu hình Quy cách đóng gói theo Model (US-24) — bảng + modal tạo/sửa/upload/download mẫu tem.
 * Dùng chung 1 API với `Station.Wpf` (AC6, Tổ trưởng nâng quyền tại trạm) — không có phiên bản riêng ở đây.
 */
export function PackingModelConfigListPage() {
  useDocumentTitle('Cấu hình đóng gói theo Model');

  const hasPermission = useAuthStore((state) => state.hasPermission);
  const configsQuery = usePackingModelConfigs();
  const downloadMutation = useDownloadPackingTemplate();

  const [formOpen, setFormOpen] = useState(false);
  const [editingConfig, setEditingConfig] = useState<PackingModelConfig | null>(null);

  const existingModels = useMemo(() => (configsQuery.data ?? []).map((c) => c.model), [configsQuery.data]);

  const openCreateForm = () => {
    setEditingConfig(null);
    setFormOpen(true);
  };

  const openEditForm = (config: PackingModelConfig) => {
    setEditingConfig(config);
    setFormOpen(true);
  };

  const handleDownloadTemplate = async (config: PackingModelConfig) => {
    try {
      const blob = await downloadMutation.mutateAsync(config.id);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `mau-tem-${config.model}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      void message.error('Tải xuống mẫu tem thất bại, vui lòng thử lại');
    }
  };

  const columns: ColumnsType<PackingModelConfig> = [
    { title: 'Model', dataIndex: 'model', key: 'model' },
    { title: 'Quy cách (SP/thùng)', dataIndex: 'packingQuantity', key: 'packingQuantity' },
    {
      title: 'Khối lượng',
      dataIndex: 'grossWeight',
      key: 'grossWeight',
      render: (grossWeight: number | null) => (grossWeight ?? '—'),
    },
    { title: 'Tên sản phẩm', dataIndex: 'partName', key: 'partName' },
    {
      title: 'Nhà sản xuất',
      dataIndex: 'manufacturer',
      key: 'manufacturer',
      render: (manufacturer: string | null) => manufacturer ?? '—',
    },
    {
      title: 'Mẫu tem',
      dataIndex: 'hasTemplate',
      key: 'hasTemplate',
      render: (hasTemplate: boolean) => <Tag color={hasTemplate ? 'green' : 'default'}>{hasTemplate ? 'Đã có' : 'Chưa có'}</Tag>,
    },
    {
      title: 'Thao tác',
      key: 'actions',
      render: (_: unknown, config: PackingModelConfig) => (
        <div style={{ display: 'flex', gap: 8 }}>
          {hasPermission('PackingModelConfig.Update') && (
            <Button size="small" onClick={() => openEditForm(config)}>
              Sửa
            </Button>
          )}
          {config.hasTemplate && (
            <Button size="small" onClick={() => void handleDownloadTemplate(config)} loading={downloadMutation.isPending}>
              Tải mẫu tem
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Cấu hình đóng gói theo Model
        </Typography.Title>
        {hasPermission('PackingModelConfig.Create') && (
          <Button type="primary" icon={<PlusOutlined />} onClick={openCreateForm}>
            Thêm cấu hình
          </Button>
        )}
      </div>
      <Table<PackingModelConfig>
        rowKey="id"
        columns={columns}
        dataSource={configsQuery.data ?? []}
        loading={configsQuery.isLoading}
      />
      <PackingModelConfigFormModal open={formOpen} editingConfig={editingConfig} existingModels={existingModels} onClose={() => setFormOpen(false)} />
    </div>
  );
}
