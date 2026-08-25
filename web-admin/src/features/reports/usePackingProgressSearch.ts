import { useQuery } from '@tanstack/react-query';
import { searchPackingProgress } from '../../api/packingProgressReportsApi';

/**
 * `GET /api/v1/reports/packing-progress/search?q=...` (US-26 AC1) — chỉ gọi khi `search` có nội dung (đã trim),
 * cùng pattern `useLotSearch` (US-21). Debounce 300ms xử lý ở component gọi hook này (`PackingProgressTab`).
 */
export function usePackingProgressSearch(search: string) {
  const trimmed = search.trim();

  return useQuery({
    queryKey: ['packing-progress-search', trimmed],
    queryFn: () => searchPackingProgress(trimmed),
    enabled: trimmed.length > 0,
  });
}
