/**
 * Type khớp DTO BreakWindow backend (`ProductionMES.Application/DTOs/BreakWindows/`, US-01a).
 * `startTime`/`endTime` serialize dạng chuỗi giờ trong ngày "HH:mm:ss" (System.Text.Json mặc định cho
 * `TimeOnly`, không có phần ngày) — khác `Utc` DateTime khác trong hệ thống.
 */

/** Khớp `BreakWindowDto`. */
export interface BreakWindow {
  id: number;
  lineId: number;
  startTime: string;
  endTime: string;
  note: string | null;
}

/** Khớp `CreateBreakWindowRequest`. */
export interface CreateBreakWindowRequest {
  startTime: string;
  endTime: string;
  note?: string | null;
}

/** Khớp `UpdateBreakWindowRequest`. */
export interface UpdateBreakWindowRequest {
  startTime: string;
  endTime: string;
  note?: string | null;
}
