import { httpClient } from './httpClient';
import type { CreateWorkStationRequest, UpdateWorkStationRequest, WorkStation } from '../types/workStation';

const BASE_PATH = '/api/v1/work-stations';

/** `GET /api/v1/work-stations` — toàn bộ WorkStation (kể cả đã vô hiệu hóa), không phân trang (danh mục nhỏ). */
export async function getAll(): Promise<WorkStation[]> {
  const response = await httpClient.get<WorkStation[]>(BASE_PATH);
  return response.data;
}

/** `POST /api/v1/work-stations` */
export async function create(request: CreateWorkStationRequest): Promise<WorkStation> {
  const response = await httpClient.post<WorkStation>(BASE_PATH, request);
  return response.data;
}

/** `PUT /api/v1/work-stations/{id}` */
export async function update(id: number, request: UpdateWorkStationRequest): Promise<WorkStation> {
  const response = await httpClient.put<WorkStation>(`${BASE_PATH}/${id}`, request);
  return response.data;
}

/** `POST /api/v1/work-stations/{id}/deactivate` — soft-delete, trả `204 No Content`. */
export async function deactivate(id: number): Promise<void> {
  await httpClient.post(`${BASE_PATH}/${id}/deactivate`);
}
