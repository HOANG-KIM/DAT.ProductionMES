/**
 * Type khớp DTO StationApiKey backend (`ProductionMES.Application/DTOs/StationApiKeys/`, US-04a/ADR-005).
 */

/** Khớp `StationApiKeyDto` — chỉ metadata, không bao giờ có giá trị thô/hash (AC2). */
export interface StationApiKey {
  id: number;
  workStationId: number;
  createdAtUtc: string;
  revokedAtUtc: string | null;
  isActive: boolean;
}

/** Khớp `IssuedStationApiKeyDto` — CHỈ xuất hiện đúng 1 lần ngay sau khi cấp/cấp lại (AC1/AC2/AC4). */
export interface IssuedStationApiKey {
  id: number;
  workStationId: number;
  createdAtUtc: string;
  apiKey: string;
}
