import { httpClient } from './httpClient';
import type { CreateStageRequest, Stage, UpdateStageRequest } from '../types/stage';

const BASE_PATH = '/api/v1/stages';

/** `GET /api/v1/stages` — toàn bộ Stage (kể cả đã vô hiệu hóa), không phân trang (danh mục nhỏ). */
export async function getAll(): Promise<Stage[]> {
  const response = await httpClient.get<Stage[]>(BASE_PATH);
  return response.data;
}

/** `POST /api/v1/stages` */
export async function create(request: CreateStageRequest): Promise<Stage> {
  const response = await httpClient.post<Stage>(BASE_PATH, request);
  return response.data;
}

/** `PUT /api/v1/stages/{id}` */
export async function update(id: number, request: UpdateStageRequest): Promise<Stage> {
  const response = await httpClient.put<Stage>(`${BASE_PATH}/${id}`, request);
  return response.data;
}

/** `POST /api/v1/stages/{id}/deactivate` — soft-delete, trả `204 No Content`. */
export async function deactivate(id: number): Promise<void> {
  await httpClient.post(`${BASE_PATH}/${id}/deactivate`);
}
