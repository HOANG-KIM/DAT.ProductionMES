import { useQuery } from '@tanstack/react-query';
import { getPackingProgressReport } from '../../api/packingProgressReportsApi';
import type { PackingProgressReportQuery } from '../../types/packingProgressReport';

/**
 * `GET /api/v1/reports/packing-progress` (US-26 AC2/AC3/AC4) — viết lại 25/08/2026: KHÔNG còn polling định kỳ
 * (AC5 — bỏ hẳn cơ chế 15s cũ), chỉ gọi khi đã chọn 1 Lot cụ thể ở AC1 (`query.lot` có giá trị), cùng pattern
 * `useLotSummary` (US-21, `enabled: selectedLot !== null`). Muốn xem số liệu mới nhất phải chọn lại Lot.
 */
export function usePackingProgressReport(query: PackingProgressReportQuery) {
  return useQuery({
    queryKey: ['packing-progress-report', query],
    queryFn: () => getPackingProgressReport(query),
    enabled: !!query.lot,
  });
}
