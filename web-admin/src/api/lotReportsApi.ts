import { httpClient } from './httpClient';
import type { LotSearchItem, LotSummary, LotSummaryQuery } from '../types/lotReport';

const BASE_PATH = '/api/v1/reports/lots';

/** `GET /api/v1/reports/lots?search=...` (US-21 AC1/AC2) — trả mảng rỗng nếu không khớp Lot nào. */
export async function searchLots(search: string): Promise<LotSearchItem[]> {
  const response = await httpClient.get<LotSearchItem[]>(BASE_PATH, { params: { search } });
  return response.data;
}

/**
 * `GET /api/v1/reports/lots/{lot}` (US-21 AC3/AC4/AC5) — ném lỗi Axios với `response.status === 404` khi Lot
 * không tồn tại (AC2 "Không tìm thấy Lot"), caller (`useLotSummary`) tự phân biệt 404 với lỗi hệ thống khác.
 */
export async function getLotSummary(lot: string, query: LotSummaryQuery): Promise<LotSummary> {
  const response = await httpClient.get<LotSummary>(`${BASE_PATH}/${encodeURIComponent(lot)}`, { params: query });
  return response.data;
}

/**
 * `GET /api/v1/reports/lots/{lot}/export` (US-23/FR-23, phạm vi thu hẹp — chỉ báo cáo "Theo Lot") — tải file
 * .xlsx (2 sheet: Tổng hợp + Chi tiết lượt scan), cùng bộ lọc đang truyền cho {@link getLotSummary}. Dùng
 * `responseType: 'blob'` vì response là file nhị phân, không phải JSON. Ném lỗi Axios với `response.status === 404`
 * khi Lot không tồn tại, giống {@link getLotSummary}.
 */
export async function exportLotReport(lot: string, query: LotSummaryQuery): Promise<Blob> {
  const response = await httpClient.get<Blob>(`${BASE_PATH}/${encodeURIComponent(lot)}/export`, {
    params: query,
    responseType: 'blob',
  });
  return response.data;
}
