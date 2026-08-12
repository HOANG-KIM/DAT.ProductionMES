/**
 * Type khớp DTO Stage backend (`ProductionMES.Application/DTOs/Stages/`, US-02/FR-02).
 */

/** Khớp `StageDto`. */
export interface Stage {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
}

/** Khớp `CreateStageRequest`. */
export interface CreateStageRequest {
  name: string;
  description?: string | null;
}

/** Khớp `UpdateStageRequest`. */
export interface UpdateStageRequest {
  name: string;
  description?: string | null;
}
