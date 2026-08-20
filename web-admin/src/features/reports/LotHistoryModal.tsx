import { Modal, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useLotHistory } from './useLotHistory';
import type { LotHistoryItem } from '../../types/lotReport';

const DATE_FORMAT = 'DD/MM/YYYY HH:mm:ss';

interface LotHistoryModalProps {
  /** `null` = đóng modal (cũng tắt query, xem `useLotHistory`). */
  lot: string | null;
  onClose: () => void;
}

/**
 * Truy vết lịch sử thay đổi "Tổng số lượng Lot" (US-21a) — vd. đặt 500 rồi sửa còn 100, ai sửa, lúc nào. Dữ liệu
 * ghi lại tại `LotService.UpsertTotalQuantityAsync` (backend), chỉ 1 dòng mỗi lần giá trị THỰC SỰ đổi. Dataset nhỏ
 * (vài lần sửa/Lot) nên không cần phân trang server-side như `ScanHistoryDrilldownDrawer`.
 */
export function LotHistoryModal({ lot, onClose }: LotHistoryModalProps) {
  const historyQuery = useLotHistory(lot);

  const columns: ColumnsType<LotHistoryItem> = [
    {
      title: 'Thời điểm',
      dataIndex: 'changedAtUtc',
      key: 'changedAtUtc',
      render: (value: string) => dayjs(value).format(DATE_FORMAT),
    },
    {
      title: 'Giá trị cũ',
      dataIndex: 'oldTotalQuantity',
      key: 'oldTotalQuantity',
      align: 'right',
      render: (value: number | null) => (value === null ? <Typography.Text type="secondary">(mới)</Typography.Text> : value.toLocaleString('vi-VN')),
    },
    {
      title: 'Giá trị mới',
      dataIndex: 'newTotalQuantity',
      key: 'newTotalQuantity',
      align: 'right',
      render: (value: number) => <Typography.Text strong>{value.toLocaleString('vi-VN')}</Typography.Text>,
    },
    {
      title: 'Người thay đổi',
      dataIndex: 'changedByUserName',
      key: 'changedByUserName',
      render: (value: string | null) => value ?? '—',
    },
  ];

  return (
    <Modal open={lot !== null} onCancel={onClose} onOk={onClose} title={`Lịch sử thay đổi Tổng SL Lot — ${lot ?? ''}`} width={640} footer={null}>
      {historyQuery.isError && (
        <Typography.Text type="danger">Không tải được lịch sử thay đổi. Vui lòng thử lại.</Typography.Text>
      )}
      <Table<LotHistoryItem>
        rowKey={(row) => `${row.changedAtUtc}-${row.newTotalQuantity}`}
        size="small"
        loading={historyQuery.isLoading}
        columns={columns}
        dataSource={historyQuery.data ?? []}
        pagination={false}
        locale={{ emptyText: 'Lot này chưa từng bị sửa "Tổng số lượng Lot" lần nào' }}
      />
    </Modal>
  );
}
