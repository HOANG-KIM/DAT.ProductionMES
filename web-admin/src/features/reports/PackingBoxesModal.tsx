import { Button, Modal, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useState } from 'react';
import { usePackingProgressBoxes } from './usePackingProgressBoxes';
import { PackingBoxScansModal } from './PackingBoxScansModal';
import type { PackingProgressReportBox } from '../../types/packingProgressReport';

const DATE_FORMAT = 'DD/MM/YYYY HH:mm:ss';

interface PackingBoxesModalProps {
  /** `null` = đóng modal (cũng tắt query, xem `usePackingProgressBoxes`). */
  filter: { lineId: number; lot: string; lineName: string } | null;
  onClose: () => void;
}

/**
 * US-26 AC6 — danh sách TẤT CẢ thùng (Completed lẫn InProgress) của 1 dòng báo cáo (Line + Lot), gộp theo TẤT CẢ
 * ProductionPlan cùng Lot (kể cả kế hoạch cũ đã Cancelled — xem `PackingProgressReportService.GetBoxesAsync`).
 * Click vào 1 thùng để xem chi tiết lượt scan (AC7/AC8, `PackingBoxScansModal`).
 */
export function PackingBoxesModal({ filter, onClose }: PackingBoxesModalProps) {
  const boxesQuery = usePackingProgressBoxes(filter ? { lineId: filter.lineId, lot: filter.lot } : null);
  const [selectedBox, setSelectedBox] = useState<{ id: number; boxNo: number } | null>(null);

  const columns: ColumnsType<PackingProgressReportBox> = [
    { title: 'Số thùng', dataIndex: 'boxNo', key: 'boxNo', width: 100 },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (value: PackingProgressReportBox['status']) =>
        value === 'Completed' ? <Tag color="success">Hoàn tất</Tag> : <Tag color="processing">Đang đóng</Tag>,
    },
    {
      title: 'Đã quét / Mục tiêu',
      key: 'quantity',
      align: 'right',
      render: (_, row) => `${row.scannedQuantity.toLocaleString('vi-VN')} / ${row.targetQuantity.toLocaleString('vi-VN')}`,
    },
    {
      title: 'Thời gian bắt đầu',
      dataIndex: 'openedAtUtc',
      key: 'openedAtUtc',
      render: (value: string) => dayjs(value).format(DATE_FORMAT),
    },
    {
      title: 'Thời gian kết thúc',
      dataIndex: 'completedAtUtc',
      key: 'completedAtUtc',
      render: (value: string | null) => (value ? dayjs(value).format(DATE_FORMAT) : '—'),
    },
    {
      title: '',
      key: 'action',
      width: 140,
      render: (_, row) => (
        <Button size="small" onClick={() => setSelectedBox({ id: row.id, boxNo: row.boxNo })}>
          Xem lượt scan
        </Button>
      ),
    },
  ];

  return (
    <>
      <Modal
        open={filter !== null}
        onCancel={onClose}
        onOk={onClose}
        title={filter ? `Danh sách thùng — Line ${filter.lineName} / Lot ${filter.lot}` : ''}
        width={860}
        footer={null}
      >
        {boxesQuery.isError && (
          <Typography.Text type="danger">Không tải được danh sách thùng. Vui lòng thử lại.</Typography.Text>
        )}
        <Table<PackingProgressReportBox>
          rowKey="id"
          size="small"
          loading={boxesQuery.isLoading}
          columns={columns}
          dataSource={boxesQuery.data ?? []}
          pagination={false}
          locale={{ emptyText: 'Chưa có thùng nào cho Lot này' }}
        />
      </Modal>

      <PackingBoxScansModal
        boxId={selectedBox?.id ?? null}
        boxNo={selectedBox?.boxNo ?? null}
        onClose={() => setSelectedBox(null)}
      />
    </>
  );
}
