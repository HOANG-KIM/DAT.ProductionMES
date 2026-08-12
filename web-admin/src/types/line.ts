/**
 * Type khớp DTO Line backend (`ProductionMES.Application/DTOs/Lines/`, US-01/FR-01).
 */

/** Khớp `LineDto`. */
export interface Line {
  id: number;
  name: string;
  description: string | null;
  isActive: boolean;
}

/** Khớp `CreateLineRequest`. */
export interface CreateLineRequest {
  name: string;
  description?: string | null;
}

/** Khớp `UpdateLineRequest`. */
export interface UpdateLineRequest {
  name: string;
  description?: string | null;
}
