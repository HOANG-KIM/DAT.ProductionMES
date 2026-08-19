import { useQuery } from '@tanstack/react-query';
import { isAxiosError } from 'axios';
import { getLotSummary } from '../../api/lotReportsApi';
import type { LotSummaryQuery } from '../../types/lotReport';

/**
 * `GET /api/v1/reports/lots/{lot}` (US-21 AC3/AC4/AC5) — `enabled: false` khi chưa chọn Lot nào (`lot === null`).
 * 404 (AC2 "Không tìm thấy Lot") KHÔNG retry (không phải lỗi tạm thời) — component tự phân biệt qua
 * `isNotFound` thay vì hiển thị như lỗi hệ thống chung.
 */
export function useLotSummary(lot: string | null, query: LotSummaryQuery) {
  const result = useQuery({
    queryKey: ['lot-summary', lot, query],
    queryFn: () => getLotSummary(lot as string, query),
    enabled: lot !== null,
    retry: (failureCount, error) => {
      if (isAxiosError(error) && error.response?.status === 404) {
        return false;
      }
      return failureCount < 3;
    },
  });

  const isNotFound = isAxiosError(result.error) && result.error.response?.status === 404;

  return { ...result, isNotFound };
}
