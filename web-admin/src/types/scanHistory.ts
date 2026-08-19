/**
 * Type khớp DTO tra cứu lịch sử scan backend (`ProductionMES.Application/DTOs/Scans/`, US-10; mở rộng 18/08/2026
 * phục vụ drill-down US-21 AC7/AC8).
 */

/** Khớp enum `ScanResult` (serialize dạng chuỗi — API-Conventions.md mục 10). */
export type ScanResult = 'Ok' | 'DuplicateTag' | 'PreviousStageNotPassed' | 'Ng' | 'WaitingReworkUnlock';

/** Khớp enum `ReworkStatus` (US-21 AC10, serialize dạng chuỗi). */
export type ReworkStatus = 'NotUnlocked' | 'WaitingRescan' | 'Fixed' | 'StillNg';

/** Khớp `ScanHistoryItemDto`. */
export interface ScanHistoryItem {
  id: number;
  tagCode: string;
  stageId: number;
  lineId: number;
  workStationId: number;
  /** US-21 AC8 — tên trạm làm việc thực hiện, KHÔNG phải tên Operator. */
  workStationName: string;
  productionPlanId: number;
  customer: string;
  model: string;
  lot: string;
  revision: string | null;
  plannedQuantity: number;
  taktTimeSeconds: number;
  /** US-21 AC12 — snapshot ProductionPlan.OperatorNames tại thời điểm scan, KHÔNG phải định danh cá nhân theo lượt. */
  operatorNames: string;
  scannedAtUtc: string;
  result: ScanResult;
  /** Lý do NG (AC9) — null khi `result` != 'Ng'. */
  rejectionReason: string | null;
  /** Người xác nhận NG (AC9, US-18) — null khi `result` != 'Ng' hoặc bản ghi Ng cũ trước 18/08/2026. */
  confirmedByUserId: number | null;
  confirmedByUserName: string | null;
  /** US-21 AC10 — null khi `result` != 'Ng'. */
  reworkStatus: ReworkStatus | null;
  /** US-21 AC10 "lần N" — chỉ có giá trị khi `reworkStatus` = 'StillNg'. */
  reworkStillNgOccurrence: number | null;
  /** US-21 AC11 — "Người sửa hàng" (= người đăng nhập mở khóa rework, US-19 AC7) — null khi `reworkStatus` null/'NotUnlocked'. */
  reworkUnlockedByUserName: string | null;
  reworkUnlockedAtUtc: string | null;
  reworkUnlockNote: string | null;
}

/** Khớp envelope `PagedResult<ScanHistoryItemDto>` (API-Conventions.md mục 9). */
export interface PagedResult<TItem> {
  items: TItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Khớp query string `GET /api/v1/scans/history` (US-10 AC2/AC3; mở rộng 18/08/2026 — US-21 AC7). */
export interface ScanHistoryQuery {
  tagCode?: string;
  workStationId?: number;
  lineId?: number;
  stageId?: number;
  lot?: string;
  model?: string;
  customer?: string;
  revision?: string;
  /** UTC ISO 8601. */
  from?: string;
  /** UTC ISO 8601. */
  to?: string;
  page?: number;
  pageSize?: number;
}
