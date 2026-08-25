import { useQuery } from '@tanstack/react-query';
import { getPackingProgressBoxes } from '../../api/packingProgressReportsApi';

export interface PackingProgressBoxesFilter {
  lineId: number;
  lot: string;
}

/**
 * `GET /api/v1/reports/packing-progress/boxes` (US-26 AC6) — dùng cho drill-down từ 1 dòng báo cáo. `enabled: false`
 * khi chưa có dòng nào được chọn (`filter === null`) — modal chỉ mở khi người dùng bấm xem chi tiết 1 dòng.
 */
export function usePackingProgressBoxes(filter: PackingProgressBoxesFilter | null) {
  return useQuery({
    queryKey: ['packing-progress-boxes', filter],
    queryFn: () => getPackingProgressBoxes(filter!.lineId, filter!.lot),
    enabled: filter !== null,
  });
}
