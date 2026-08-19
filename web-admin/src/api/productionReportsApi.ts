import { httpClient } from './httpClient';
import type { ProductionReportQuery, ProductionReportResponse } from '../types/productionReport';

const BASE_PATH = '/api/v1/reports/production-summary';

/** `GET /api/v1/reports/production-summary` (US-21) — `lineId`/`from`/`to` đều tùy chọn, kết hợp AND. */
export async function getProductionReport(query: ProductionReportQuery): Promise<ProductionReportResponse> {
  const response = await httpClient.get<ProductionReportResponse>(BASE_PATH, { params: query });
  return response.data;
}
