import { httpClient } from './httpClient';
import type {
  PackingProgressReportBox,
  PackingProgressReportBoxScans,
  PackingProgressReportQuery,
  PackingProgressReportResponse,
  PackingProgressSearchItem,
} from '../types/packingProgressReport';

const BASE_PATH = '/api/v1/reports/packing-progress';

/**
 * `GET /api/v1/reports/packing-progress/search?q=...` (US-26 AC1, viết lại 25/08/2026) — gợi ý Lot đang Running
 * tại công đoạn "Đóng thùng", KHÔNG lọc theo Line. Trả mảng rỗng nếu không khớp Lot nào.
 */
export async function searchPackingProgress(q: string): Promise<PackingProgressSearchItem[]> {
  const response = await httpClient.get<PackingProgressSearchItem[]>(`${BASE_PATH}/search`, { params: { q } });
  return response.data;
}

/** `GET /api/v1/reports/packing-progress` (US-26 AC2/AC3/AC4) — `lineId`/`lot`/`model` đều tùy chọn, kết hợp AND. */
export async function getPackingProgressReport(query: PackingProgressReportQuery): Promise<PackingProgressReportResponse> {
  const response = await httpClient.get<PackingProgressReportResponse>(BASE_PATH, { params: query });
  return response.data;
}

/** `GET /api/v1/reports/packing-progress/boxes` (US-26 AC6) — danh sách TẤT CẢ thùng của 1 dòng báo cáo (Line + Lot). */
export async function getPackingProgressBoxes(lineId: number, lot: string): Promise<PackingProgressReportBox[]> {
  const response = await httpClient.get<PackingProgressReportBox[]>(`${BASE_PATH}/boxes`, { params: { lineId, lot } });
  return response.data;
}

/** `GET /api/v1/reports/packing-progress/boxes/{boxId}/scans` (US-26 AC7/AC8) — danh sách lượt scan OK đã cộng vào 1 thùng cụ thể. */
export async function getPackingProgressBoxScans(boxId: number): Promise<PackingProgressReportBoxScans> {
  const response = await httpClient.get<PackingProgressReportBoxScans>(`${BASE_PATH}/boxes/${boxId}/scans`);
  return response.data;
}

/**
 * `GET /api/v1/reports/packing-progress/export` (US-26 AC9-AC13) — tải file .xlsx (3 sheet: Tổng quan/Danh sách
 * thùng/Lượt scan) cho ĐÚNG 1 dòng báo cáo (Line + Lot), hành động theo TỪNG DÒNG (không phải nút xuất chung toàn
 * bảng). Dùng `responseType: 'blob'` vì response là file nhị phân, không phải JSON. Ném lỗi Axios với
 * `response.status === 404` khi không còn dòng nào khớp Line + Lot tại thời điểm xuất.
 */
export async function exportPackingProgressReport(lineId: number, lot: string): Promise<Blob> {
  const response = await httpClient.get<Blob>(`${BASE_PATH}/export`, {
    params: { lineId, lot },
    responseType: 'blob',
  });
  return response.data;
}
