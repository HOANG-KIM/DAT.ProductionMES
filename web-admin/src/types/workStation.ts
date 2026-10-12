/**
 * Type khớp DTO WorkStation backend (`ProductionMES.Application/DTOs/WorkStations/`, US-04/FR-04).
 */

/** Khớp `WorkStationDto`. */
export interface WorkStation {
  id: number;
  name: string;
  lineId: number;
  stageId: number;
  /** "Trạm có máy thông điện". */
  useArduino: boolean;
  /** Không còn dùng (US-11/AC12). */
  comPort: string | null;
  baudRate: number | null;
  commandProtocol: string | null;
  /** Công tắc runtime "Đang sử dụng máy thông điện" (US-11) — chỉ đọc, Tổ trưởng đổi tại trạm. */
  testerEnabled: boolean;
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
