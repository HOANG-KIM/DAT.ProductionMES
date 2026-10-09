/**
 * Type khớp DTO PackingModelConfig backend (`ProductionMES.Application/DTOs/PackingModelConfigs/`, US-24/FR-24).
 */

/** Khớp `PackingModelConfigDto`. */
export interface PackingModelConfig {
  id: number;
  model: string;
  packingQuantity: number;
  grossWeight: number | null;
  partName: string;
  manufacturer: string | null;
  /** true nếu đã có file mẫu tem (template .xlsx) được tải lên (AC3). */
  hasTemplate: boolean;
  templateUpdatedAtUtc: string | null;
  templateUpdatedByUserName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  updatedByUserName: string | null;
}

/** Khớp `CreatePackingModelConfigRequest`. */
export interface CreatePackingModelConfigRequest {
  model: string;
  packingQuantity: number;
  grossWeight?: number | null;
  partName: string;
  manufacturer?: string | null;
}

/** Khớp `UpdatePackingModelConfigRequest` — KHÔNG có `model` (không đổi được sau khi tạo, xem AC2). */
export interface UpdatePackingModelConfigRequest {
  packingQuantity: number;
  grossWeight?: number | null;
  partName: string;
  manufacturer?: string | null;
}

/** Khớp `PackingLabelTokenDto` (AC10) — 1 token hợp lệ dùng trong file mẫu tem, hiển thị tham chiếu trên UI. */
export interface PackingLabelToken {
  token: string;
  description: string;
}

/** Khớp `PackingTemplateWarningDto` (AC10) — 1 dòng cảnh báo token không hợp lệ khi quét file mẫu tem vừa tải lên. */
export interface PackingTemplateWarning {
  message: string;
  /** Địa chỉ ô dạng "TênSheet!B2" nếu xác định được vị trí cụ thể — `null` cho cảnh báo tổng quát. */
  cellReference: string | null;
}

/** Khớp `UploadPackingTemplateResultDto` (AC10) — kết quả tải lên mẫu tem, KHÔNG chặn lưu dù có cảnh báo. */
export interface UploadPackingTemplateResult {
  config: PackingModelConfig;
  warnings: PackingTemplateWarning[];
}
