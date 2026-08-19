import { httpClient } from './httpClient';
import type { PagedResult, ScanHistoryItem, ScanHistoryQuery } from '../types/scanHistory';

const BASE_PATH = '/api/v1/scans/history';

/**
 * `GET /api/v1/scans/history` (US-10 AC2/AC3; mở rộng 18/08/2026 phục vụ drill-down US-21 AC7) — mọi filter đều
 * tùy chọn, kết hợp AND.
 */
export async function getScanHistory(query: ScanHistoryQuery): Promise<PagedResult<ScanHistoryItem>> {
  const response = await httpClient.get<PagedResult<ScanHistoryItem>>(BASE_PATH, { params: query });
  return response.data;
}
