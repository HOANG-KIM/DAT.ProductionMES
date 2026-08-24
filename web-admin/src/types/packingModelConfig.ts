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
