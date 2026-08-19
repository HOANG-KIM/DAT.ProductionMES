import { useQuery } from '@tanstack/react-query';
import { searchLots } from '../../api/lotReportsApi';

/** `GET /api/v1/reports/lots?search=...` (US-21 AC1/AC2) — chỉ gọi khi `search` có nội dung (đã trim). */
export function useLotSearch(search: string) {
  const trimmed = search.trim();

  return useQuery({
    queryKey: ['lot-search', trimmed],
    queryFn: () => searchLots(trimmed),
    enabled: trimmed.length > 0,
  });
}
