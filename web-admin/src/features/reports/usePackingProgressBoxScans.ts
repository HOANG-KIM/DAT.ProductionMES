import { useQuery } from '@tanstack/react-query';
import { getPackingProgressBoxScans } from '../../api/packingProgressReportsApi';

/**
 * `GET /api/v1/reports/packing-progress/boxes/{boxId}/scans` (US-26 AC7/AC8) — dùng cho drill-down từ danh sách
 * thùng (AC6). `enabled: false` khi chưa chọn thùng nào (`boxId === null`).
 */
export function usePackingProgressBoxScans(boxId: number | null) {
  return useQuery({
    queryKey: ['packing-progress-box-scans', boxId],
    queryFn: () => getPackingProgressBoxScans(boxId as number),
    enabled: boxId !== null,
  });
}
