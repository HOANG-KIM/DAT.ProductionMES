import { Tabs, Typography } from 'antd';
import { LotReportTab } from './LotReportTab';
import { LineReportTab } from './LineReportTab';
import { useDocumentTitle } from '../../hooks/useDocumentTitle';

/**
 * Màn hình báo cáo (US-21, vòng 3 — 18/08/2026, Lot-centric). Tab mặc định "Theo Lot" (AC1-AC5/AC7-AC11) là
 * entrypoint chính theo yêu cầu Ban quản lý — tìm/chọn 1 Lot rồi xem toàn bộ vòng đời sản xuất, không cần biết
 * trước Lot chạy ở Line nào. Tab phụ "Theo Line (PLAN/BALANCE)" giữ lại nguyên vẹn báo cáo vòng 1/2 (group (Line,
 * Công đoạn, Lot) kèm PLAN/BALANCE) làm nhánh phụ theo AC6 — không bắt buộc, chỉ giữ để tận dụng code đã có.
 */
export function ProductionReportPage() {
  useDocumentTitle('Báo cáo theo Lot');

  return (
    <div>
      <Typography.Title level={3} style={{ marginTop: 0, marginBottom: 16 }}>
        Báo cáo theo Lot
      </Typography.Title>

      <Tabs
        defaultActiveKey="lot"
        items={[
          { key: 'lot', label: 'Theo Lot', children: <LotReportTab /> },
          { key: 'line', label: 'Theo Line (PLAN/BALANCE)', children: <LineReportTab /> },
        ]}
      />
    </div>
  );
}
