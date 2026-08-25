import { Alert, Modal, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { usePackingProgressBoxScans } from './usePackingProgressBoxScans';
import type { PackingProgressReportBoxScan } from '../../types/packingProgressReport';

const DATE_FORMAT = 'DD/MM/YYYY HH:mm:ss';

interface PackingBoxScansModalProps {
  /** `null` = đóng modal (cũng tắt query, xem `usePackingProgressBoxScans`). */
  boxId: number | null;
  boxNo: number | null;
  onClose: () => void;
}

/**
 * US-26 AC7/AC8 — chi tiết các lượt scan OK đã cộng vào 1 thùng cụ thể (KHÔNG hiển thị lượt bị từ chối, đã lọc ở
 * backend). AC8: khi `hasDetailedScanData = false` (thùng mở/hoàn tất TRƯỚC thời điểm triển khai `Scan.PackingBoxId`),
 * hiển thị rõ thông báo "Không có dữ liệu chi tiết" thay vì bảng rỗng — tránh hiểu nhầm là thùng có 0 lượt scan thật.
 */
export function PackingBoxScansModal({ boxId, boxNo, onClose }: PackingBoxScansModalProps) {
  const scansQuery = usePackingProgressBoxScans(boxId);
  const data = scansQuery.data;

  const columns: ColumnsType<PackingProgressReportBoxScan> = [
    { title: 'Mã tem', dataIndex: 'tagCode', key: 'tagCode' },
    {
      title: 'Thời điểm scan',
      dataIndex: 'scannedAtUtc',
      key: 'scannedAtUtc',
      render: (value: string) => dayjs(value).format(DATE_FORMAT),
    },
  ];

  return (
    <Modal open={boxId !== null} onCancel={onClose} onOk={onClose} title={`Chi tiết lượt scan — Thùng số ${boxNo ?? ''}`} width={640} footer={null}>
      {scansQuery.isError && (
        <Typography.Text type="danger">Không tải được chi tiết lượt scan. Vui lòng thử lại.</Typography.Text>
      )}

      {data && !data.hasDetailedScanData ? (
        <Alert
          type="warning"
          showIcon
          message="Không có dữ liệu chi tiết lượt scan"
          description="Thùng này được mở/hoàn tất trước khi hệ thống lưu chi tiết từng lượt scan theo thùng — không thể tra cứu lại."
        />
      ) : (
        <Table<PackingProgressReportBoxScan>
          rowKey={(row) => `${row.tagCode}-${row.scannedAtUtc}`}
          size="small"
          loading={scansQuery.isLoading}
          columns={columns}
          dataSource={data?.scans ?? []}
          pagination={false}
          locale={{ emptyText: 'Thùng này chưa có lượt scan OK nào' }}
        />
      )}
    </Modal>
  );
}
