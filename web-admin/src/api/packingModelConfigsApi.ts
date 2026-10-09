import { httpClient } from './httpClient';
import type {
  CreatePackingModelConfigRequest,
  PackingLabelToken,
  PackingModelConfig,
  UpdatePackingModelConfigRequest,
  UploadPackingTemplateResult,
} from '../types/packingModelConfig';

const BASE_PATH = '/api/v1/packing-model-configs';

/** `GET /api/v1/packing-model-configs` (AC3) — toàn bộ cấu hình, không phân trang (danh mục nhỏ). */
export async function getAll(): Promise<PackingModelConfig[]> {
  const response = await httpClient.get<PackingModelConfig[]>(BASE_PATH);
  return response.data;
}

/** `POST /api/v1/packing-model-configs` (AC1) — 409 nếu Model đã có cấu hình (so khớp AC9). */
export async function create(request: CreatePackingModelConfigRequest): Promise<PackingModelConfig> {
  const response = await httpClient.post<PackingModelConfig>(BASE_PATH, request);
  return response.data;
}

/** `PUT /api/v1/packing-model-configs/{id}` (AC2) — không đổi Model. */
export async function update(id: number, request: UpdatePackingModelConfigRequest): Promise<PackingModelConfig> {
  const response = await httpClient.put<PackingModelConfig>(`${BASE_PATH}/${id}`, request);
  return response.data;
}

/** `GET /api/v1/packing-model-configs/suggest-models?search=...` (AC9) — gợi ý autocomplete Model đã có cấu hình. */
export async function suggestModels(search: string): Promise<string[]> {
  const response = await httpClient.get<string[]>(`${BASE_PATH}/suggest-models`, { params: { search } });
  return response.data;
}

/**
 * `POST /api/v1/packing-model-configs/{id}/template` (AC4) — tải lên (thay thế) file mẫu tem, `multipart/form-data`.
 * Không tự set header `Content-Type` — Axios/trình duyệt tự thêm `boundary` đúng khi truyền `FormData`.
 * AC10: KHÔNG chặn lưu — response luôn kèm `warnings` (có thể rỗng) khi phát hiện token không hợp lệ.
 */
export async function uploadTemplate(id: number, file: File): Promise<UploadPackingTemplateResult> {
  const formData = new FormData();
  formData.append('file', file);
  const response = await httpClient.post<UploadPackingTemplateResult>(`${BASE_PATH}/${id}/template`, formData);
  return response.data;
}

/** `GET /api/v1/packing-model-configs/{id}/template` (AC5) — tải file mẫu tem đang cấu hình, `responseType: 'blob'` (cùng idiom `exportLotReport`, US-23). */
export async function downloadTemplate(id: number): Promise<Blob> {
  const response = await httpClient.get<Blob>(`${BASE_PATH}/${id}/template`, { responseType: 'blob' });
  return response.data;
}

/** `GET /api/v1/packing-model-configs/template-tokens` (AC10) — danh sách 9 token hợp lệ dùng trong file mẫu tem, hiển thị tham chiếu trên UI. */
export async function getTemplateTokens(): Promise<PackingLabelToken[]> {
  const response = await httpClient.get<PackingLabelToken[]>(`${BASE_PATH}/template-tokens`);
  return response.data;
}
