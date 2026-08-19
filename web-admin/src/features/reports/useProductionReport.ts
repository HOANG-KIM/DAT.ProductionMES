import { useQuery } from '@tanstack/react-query';
import { getProductionReport } from '../../api/productionReportsApi';
import type { ProductionReportQuery } from '../../types/productionReport';

/**
 * AC1 "xem theo thời gian thực": polling định kỳ mỗi 15s (giả định kỹ thuật — SRS/FR-21 không quy định độ trễ
 * cụ thể cho màn hình báo cáo, khác NFR real-time ≤ 1s áp dụng cho SignalR ở màn hình trạm US-09; polling đơn
 * giản là đủ cho 1 màn hình tổng hợp toàn nhà máy, không cần hạ tầng SignalR riêng — xem ghi chú US-21 trong
 * BACKLOG-user-story.md). Chỉ polling khi KHÔNG lọc theo khoảng thời gian quá khứ (AC2) — dữ liệu quá khứ không
 * đổi, không cần làm mới.
 */
const REALTIME_POLL_INTERVAL_MS = 15_000;

/** `GET /api/v1/reports/production-summary` (US-21). */
export function useProductionReport(query: ProductionReportQuery) {
  const isRealtime = !query.from && !query.to;

  return useQuery({
    queryKey: ['production-report', query],
    queryFn: () => getProductionReport(query),
    refetchInterval: isRealtime ? REALTIME_POLL_INTERVAL_MS : false,
  });
}
