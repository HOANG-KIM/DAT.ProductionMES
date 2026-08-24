/**
 * Type khớp DTO Stage backend (`ProductionMES.Application/DTOs/Stages/`, US-02/FR-02).
 */

/** Khớp `StageDto`. */
export interface Stage {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
  /** US-25: true nếu đây là công đoạn "Đóng thùng" đặc thù (đếm số lượng, tự động in tem thùng). */
  isPackingStage: boolean;
}

/** Khớp `CreateStageRequest`. */
export interface CreateStageRequest {
  name: string;
  description?: string | null;
  isPackingStage: boolean;
}

/** Khớp `UpdateStageRequest`. */
export interface UpdateStageRequest {
  name: string;
  description?: string | null;
  isPackingStage: boolean;
}
