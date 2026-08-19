import { useQuery } from '@tanstack/react-query';
import { getScanHistory } from '../../api/scanHistoryApi';
import type { ScanHistoryQuery } from '../../types/scanHistory';

/**
 * `GET /api/v1/scans/history` — dùng cho drill-down từ 1 dòng báo cáo tổng hợp (US-21 AC7/AC8). `enabled: false`
 * khi chưa có dòng nào được chọn (`query === null`) — modal chỉ mở khi người dùng bấm xem chi tiết 1 dòng.
 */
export function useScanHistory(query: ScanHistoryQuery | null) {
  return useQuery({
    queryKey: ['scan-history', query],
    queryFn: () => getScanHistory(query ?? {}),
    enabled: query !== null,
  });
}
