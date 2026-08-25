/**
 * Type khớp DTO màn hình theo dõi tiến độ đóng thùng backend (`ProductionMES.Application/DTOs/Reports/`,
 * US-26/FR-26).
 */

/** Trạng thái kế hoạch của bản ghi đại diện — khớp enum `PlanStatus` backend (serialize dạng chuỗi, AC14). */
export type PackingProgressPlanStatus = 'Running' | 'Paused' | 'Completed' | 'Cancelled';

/**
 * Khớp `PackingProgressReportRowDto` (AC2/AC14) — 1 dòng ứng 1 cặp (Kế hoạch sản xuất, Công đoạn "Đóng thùng"),
 * đại diện theo thứ tự ưu tiên PlanStatus (viết lại LẦN 3 — 25/08/2026: Running/Paused/Completed/Cancelled, không
 * còn giới hạn chỉ Running).
 */
export interface PackingProgressReportRow {
  productionPlanId: number;
  lineId: number;
  lineName: string;
  stageId: number;
  stageName: string;
  model: string;
  lot: string;
  /** Trạng thái kế hoạch của bản ghi đại diện (AC14) — Running/Paused/Completed/Cancelled (không bao giờ Draft). */
  planStatus: PackingProgressPlanStatus;
  /** Số thùng đã đóng xong (đã gộp theo Lot khi kế hoạch cũ bị Cancelled rồi tạo lại) (AC1). */
  completedBoxCount: number;
  /** Tổng số lượng sản phẩm OK đã đóng thùng — KHÔNG tính thùng đang đóng dở (AC1). */
  packedOkQuantity: number;
  /** "Tổng số lượng Lot" nhập tay (US-21a). `null` = "Chưa xác định" (AC3). */
  lotTotalQuantity: number | null;
  /** % hoàn thành (AC2). `null` khi `lotTotalQuantity` = null (AC3 "Chưa xác định") — KHÔNG suy diễn 0%. */
  completionPercentage: number | null;
  /** Nhãn Đủ/Chưa đủ khi đạt/vượt 100% (AC2), cùng quy ước US-21a. `null` khi `lotTotalQuantity` = null. */
  isSufficientQuantity: boolean | null;
}

/** Khớp `PackingProgressReportDto`. */
export interface PackingProgressReportResponse {
  generatedAtUtc: string;
  rows: PackingProgressReportRow[];
}

/** Khớp query string `GET /api/v1/reports/packing-progress` (AC2/AC3/AC4) — thường chỉ truyền `lot` sau khi đã chọn ở AC1. */
export interface PackingProgressReportQuery {
  lineId?: number;
  lot?: string;
  model?: string;
}

/**
 * Khớp `PackingProgressSearchItemDto` (AC1, viết lại LẦN 2 — 25/08/2026) — 1 gợi ý Lot đang Running tại công đoạn
 * "Đóng thùng", gộp DUY NHẤT theo Lot (dedupe — KHÔNG lặp lại theo Line). Việc phân biệt theo Line dời xuống bảng
 * kết quả + dropdown lọc Line (AC2).
 */
export interface PackingProgressSearchItem {
  lot: string;
}

/** Trạng thái thùng — khớp enum `PackingBoxStatus` backend (serialize dạng chuỗi, API-Conventions.md mục 10). */
export type PackingBoxStatus = 'InProgress' | 'Completed';

/** Khớp `PackingProgressReportBoxDto` (AC6) — 1 thùng thuộc 1 dòng báo cáo (Line + Lot), gồm cả Completed lẫn InProgress. */
export interface PackingProgressReportBox {
  id: number;
  boxNo: number;
  status: PackingBoxStatus;
  scannedQuantity: number;
  targetQuantity: number;
  openedAtUtc: string;
  /** `null` khi `status` = `InProgress`. */
  completedAtUtc: string | null;
}

/** Khớp `PackingProgressReportBoxScanDto` (AC7) — 1 lượt scan OK đã cộng vào 1 thùng cụ thể. */
export interface PackingProgressReportBoxScan {
  tagCode: string;
  scannedAtUtc: string;
}

/** Khớp `PackingProgressReportBoxScansDto` (AC7/AC8). */
export interface PackingProgressReportBoxScans {
  /** `false` = thùng cũ (mở/hoàn tất trước khi triển khai AC7) KHÔNG có dữ liệu chi tiết lượt scan (AC8) — client PHẢI hiển thị rõ, KHÔNG hiển thị bảng rỗng gây hiểu nhầm. */
  hasDetailedScanData: boolean;
  scans: PackingProgressReportBoxScan[];
}
