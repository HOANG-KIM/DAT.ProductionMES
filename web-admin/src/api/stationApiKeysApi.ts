import { isAxiosError } from 'axios';
import { httpClient } from './httpClient';
import type { IssuedStationApiKey, StationApiKey } from '../types/stationApiKey';

const basePath = (workStationId: number) => `/api/v1/work-stations/${workStationId}/api-key`;

/** `GET /api/v1/work-stations/{id}/api-key` — trả `null` khi trạm chưa từng được cấp key nào (`404`, AC2). */
export async function getCurrent(workStationId: number): Promise<StationApiKey | null> {
  try {
    const response = await httpClient.get<StationApiKey>(basePath(workStationId));
    return response.data;
  } catch (error) {
    if (isAxiosError(error) && error.response?.status === 404) {
      return null;
    }
    throw error;
  }
}

/** `POST /api/v1/work-stations/{id}/api-key` — trả giá trị thô đúng 1 lần (AC1). */
export async function issue(workStationId: number): Promise<IssuedStationApiKey> {
  const response = await httpClient.post<IssuedStationApiKey>(basePath(workStationId));
  return response.data;
}

/** `POST /api/v1/work-stations/{id}/api-key/revoke` (AC3). */
export async function revoke(workStationId: number): Promise<void> {
  await httpClient.post(`${basePath(workStationId)}/revoke`);
}

/** `POST /api/v1/work-stations/{id}/api-key/reissue` — xoay vòng, trả giá trị thô mới đúng 1 lần (AC4). */
export async function reissue(workStationId: number): Promise<IssuedStationApiKey> {
  const response = await httpClient.post<IssuedStationApiKey>(`${basePath(workStationId)}/reissue`);
  return response.data;
}
