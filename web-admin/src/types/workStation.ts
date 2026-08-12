/**
 * Type khớp DTO WorkStation backend (`ProductionMES.Application/DTOs/WorkStations/`, US-04/FR-04).
 */

/** Khớp `WorkStationDto`. */
export interface WorkStation {
  id: number;
  name: string;
  lineId: number;
  stageId: number;
  useArduino: boolean;
  comPort: string | null;
  baudRate: number | null;
  commandProtocol: string | null;
  isActive: boolean;
}

/** Khớp `CreateWorkStationRequest`. */
export interface CreateWorkStationRequest {
  name: string;
  lineId: number;
  stageId: number;
  useArduino: boolean;
  comPort?: string | null;
  baudRate?: number | null;
  commandProtocol?: string | null;
}

/** Khớp `UpdateWorkStationRequest`. */
export interface UpdateWorkStationRequest {
  name: string;
  lineId: number;
  stageId: number;
  useArduino: boolean;
  comPort?: string | null;
  baudRate?: number | null;
  commandProtocol?: string | null;
}
