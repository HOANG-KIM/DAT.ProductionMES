import { Tabs, Typography } from 'antd';
import { LotReportTab } from './LotReportTab';
// import { LineReportTab } from './LineReportTab'; // Tab "Theo Line (PLAN/BALANCE)" ẩn tạm 25/08/2026 — hiện không dùng đến, xem đoạn comment bên dưới. Bật lại: bỏ comment import này + item 'line' trong Tabs.
import { PackingProgressTab } from './PackingProgressTab';
import { useDocumentTitle } from '../../hooks/useDocumentTitle';

/**
 * Màn hình báo cáo (US-21, vòng 3 — 18/08/2026, Lot-centric). Tab mặc định "Theo Lot" (AC1-AC5/AC7-AC11) là
 * entrypoint chính theo yêu cầu Ban quản lý — tìm/chọn 1 Lot rồi xem toàn bộ vòng đời sản xuất, không cần biết
 * trước Lot chạy ở Line nào.
 *
 * Tab "Theo Line (PLAN/BALANCE)" (`LineReportTab`, báo cáo vòng 1/2 — group (Line, Công đoạn, Lot) kèm PLAN/
 * BALANCE) ẨN TẠM 25/08/2026 theo yêu cầu người giao việc (hiện không dùng đến) — file/code vẫn giữ nguyên,
 * chỉ bỏ khỏi danh sách Tabs hiển thị bên dưới, bật lại được bất kỳ lúc nào khi cần (xem comment import ở trên).
 *
 * Tab "Đóng thùng" (US-26, 24/08/2026): đặt CẠNH tab "Theo Lot" (không tạo trang/route riêng) — Quyết định thiết
 * kế vì đây cùng khu vực "báo cáo cấp quản lý" (cùng permission `Report.View`, cùng đối tượng dùng Tổ trưởng/Ban
 * quản lý), không có nhu cầu điều hướng/deep-link riêng nào khác ngoài xem báo cáo, nên tận dụng layout Tabs sẵn
 * có thay vì thêm route mới trong `routes/`.
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
          // { key: 'line', label: 'Theo Line (PLAN/BALANCE)', children: <LineReportTab /> }, // ẩn tạm 25/08/2026, xem comment import ở trên.
          { key: 'packing', label: 'Đóng thùng', children: <PackingProgressTab /> },
        ]}
      />
    </div>
  );
}
