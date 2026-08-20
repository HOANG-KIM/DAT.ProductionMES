import { useQuery } from '@tanstack/react-query';
import { getLotHistory } from '../../api/lotReportsApi';

/** `GET /api/v1/reports/lots/{lot}/history` — `enabled: false` khi chưa chọn Lot nào (`lot === null`). */
export function useLotHistory(lot: string | null) {
  return useQuery({
    queryKey: ['lot-history', lot],
    queryFn: () => getLotHistory(lot as string),
    enabled: lot !== null,
  });
}
