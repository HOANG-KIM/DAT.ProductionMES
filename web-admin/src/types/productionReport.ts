/**
 * Type khớp DTO báo cáo tổng hợp theo Line/Lot/công đoạn backend (`ProductionMES.Application/DTOs/Reports/`,
 * US-21/FR-21 — mở rộng 18/08/2026, AC4-AC8).
 */

/** Khớp `ProductionReportRowDto` — 1 dòng đại diện 1 bộ (Line, Lot, Công đoạn) (AC4). */
export interface ProductionReportRow {
  lineId: number;
  lineName: string;
  stageId: number;
  stageName: string;
  /** AC4 — Lot xác định dòng này. Rỗng khi `hasPlanData` = false. */
  lot: string;
  hasPlanData: boolean;
  /** Id TẤT CẢ kế hoạch đã gộp (SUM) vào dòng này — có thể nhiều hơn 1 (Quyết định 18/08/2026). */
  productionPlanIds: number[];
  actual: number;
  /** AC5 (mới) — chỉ đếm `ScanResult.Ng`. */
  ngCount: number;
  plan: number;
  balance: number;
}

/** Khớp `ProductionReportDto`. */
export interface ProductionReportResponse {
  fromUtc: string | null;
  toUtc: string | null;
  generatedAtUtc: string;
  rows: ProductionReportRow[];
}

/** Khớp query string `GET /api/v1/reports/production-summary` (mở rộng 18/08/2026 — AC6). */
export interface ProductionReportQuery {
  lineId?: number;
  stageId?: number;
  lot?: string;
  model?: string;
  customer?: string;
  revision?: string;
  /** UTC ISO 8601 — không truyền cùng `to` = AC1 xem theo thời gian thực. */
  from?: string;
  /** UTC ISO 8601. */
  to?: string;
}
