import { httpClient } from './httpClient';
import type { CreateLineRequest, Line, UpdateLineRequest } from '../types/line';

const BASE_PATH = '/api/v1/lines';

/** `GET /api/v1/lines` — toàn bộ Line (kể cả đã vô hiệu hóa), không phân trang (danh mục nhỏ). */
export async function getAll(): Promise<Line[]> {
  const response = await httpClient.get<Line[]>(BASE_PATH);
  return response.data;
}

/** `POST /api/v1/lines` */
export async function create(request: CreateLineRequest): Promise<Line> {
  const response = await httpClient.post<Line>(BASE_PATH, request);
  return response.data;
}

/** `PUT /api/v1/lines/{id}` */
export async function update(id: number, request: UpdateLineRequest): Promise<Line> {
  const response = await httpClient.put<Line>(`${BASE_PATH}/${id}`, request);
  return response.data;
}

/** `POST /api/v1/lines/{id}/deactivate` — soft-delete, trả `204 No Content`. */
export async function deactivate(id: number): Promise<void> {
  await httpClient.post(`${BASE_PATH}/${id}/deactivate`);
}
