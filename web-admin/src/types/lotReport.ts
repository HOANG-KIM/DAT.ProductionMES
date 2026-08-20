/**
 * Type khớp DTO báo cáo Lot-centric backend (`ProductionMES.Application/DTOs/Reports/`, US-21 vòng 3 —
 * 18/08/2026, AC1-AC5; US-21a AC5/AC6, viết lại hoàn toàn 19/08/2026 — "Tổng số lượng Lot" nhập tay).
 */

/** Khớp `LotSearchItemDto` (AC1/AC2). */
export interface LotSearchItem {
  lot: string;
}

/** Khớp `LotStageRowDto` (AC4, US-21a AC5) — 1 dòng (Line, Công đoạn) trong chi tiết 1 Lot. */
export interface LotStageRow {
  lineId: number;
  lineName: string;
  stageId: number;
  stageName: string;
  okCount: number;
  ngCount: number;
  /**
   * US-21a AC5 — so `okCount` với `LotSummary.lotTotalQuantity` CỦA RIÊNG DÒNG NÀY (`okCount >= lotTotalQuantity`).
   * `null` khi `lotTotalQuantity` = null ("Chưa xác định", AC6) — không suy luận sai.
   */
  isSufficientQuantity: boolean | null;
}

/** Khớp `LotSummaryDto` (AC3/AC4/AC5, US-21a AC5/AC6). */
export interface LotSummary {
  lot: string;
  /** AC3 — >1 phần tử nghĩa là KHÔNG đồng nhất giữa các kế hoạch cùng Lot (thường do khác Line) — hiển thị cảnh báo. */
  models: string[];
  customers: string[];
  /** Chuỗi rỗng ("") đại diện Revision = null trên `ProductionPlan` nào đó. */
  revisions: string[];
  fromUtc: string | null;
  toUtc: string | null;
  rows: LotStageRow[];
  /**
   * US-21a AC1/AC5 (viết lại hoàn toàn 19/08/2026) — "Tổng số lượng Lot" NHẬP TAY (entity `Lot`, KHÔNG phải
   * SUM(PlannedQuantity) như bản đề xuất ban đầu). `null` = "Chưa xác định" (AC6).
   */
  lotTotalQuantity: number | null;
  /**
   * FR-21b (bổ sung 19/08/2026) — "Thời gian bắt đầu" = MIN(ScannedAtUtc) trên TOÀN BỘ lượt scan của Lot (mọi
   * Result, kể cả bị từ chối), KHÔNG bị ảnh hưởng bởi `fromUtc`/`toUtc`. `null` = "Chưa xác định" (chưa có scan nào).
   */
  firstScannedAtUtc: string | null;
}

/** Khớp query string `GET /api/v1/reports/lots/{lot}` (AC5). */
export interface LotSummaryQuery {
  from?: string;
  to?: string;
}

/** Khớp `LotHistoryItemDto` — 1 dòng lịch sử thay đổi "Tổng số lượng Lot" (mới nhất trước). */
export interface LotHistoryItem {
  /** `null` nếu đây là lần đầu tiên Lot này được đặt giá trị. */
  oldTotalQuantity: number | null;
  newTotalQuantity: number;
  /** Giờ tường tại nhà máy (giờ Việt Nam), KHÔNG quy đổi UTC — xem API-Conventions.md mục 10. */
  changedAtUtc: string;
  changedByUserName: string | null;
}
