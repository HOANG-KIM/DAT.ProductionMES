import { httpClient } from './httpClient';
import type { BreakWindow, CreateBreakWindowRequest, UpdateBreakWindowRequest } from '../types/breakWindow';

const basePath = (lineId: number) => `/api/v1/lines/${lineId}/break-windows`;

/** `GET /api/v1/lines/{lineId}/break-windows` — toàn bộ khung giờ nghỉ của Line (có thể rỗng). */
export async function getAll(lineId: number): Promise<BreakWindow[]> {
  const response = await httpClient.get<BreakWindow[]>(basePath(lineId));
  return response.data;
}

/** `POST /api/v1/lines/{lineId}/break-windows` */
export async function create(lineId: number, request: CreateBreakWindowRequest): Promise<BreakWindow> {
  const response = await httpClient.post<BreakWindow>(basePath(lineId), request);
  return response.data;
}

/** `PUT /api/v1/lines/{lineId}/break-windows/{id}` */
export async function update(lineId: number, id: number, request: UpdateBreakWindowRequest): Promise<BreakWindow> {
  const response = await httpClient.put<BreakWindow>(`${basePath(lineId)}/${id}`, request);
  return response.data;
}

/** `DELETE /api/v1/lines/{lineId}/break-windows/{id}` — trả `204 No Content`. */
export async function remove(lineId: number, id: number): Promise<void> {
  await httpClient.delete(`${basePath(lineId)}/${id}`);
}
