import { httpClient, setCsrfToken } from './httpClient';
import type { CsrfResponse, LoginRequest, LoginResponse } from '../types/auth';

const BASE_PATH = '/api/v1/auth';

/** Gọi trước request đổi dữ liệu đầu tiên (lúc app khởi động) — lưu csrfToken vào httpClient. */
export async function getCsrf(): Promise<void> {
  const response = await httpClient.get<CsrfResponse>(`${BASE_PATH}/csrf`);
  setCsrfToken(response.data.csrfToken);
}

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await httpClient.post<LoginResponse>(`${BASE_PATH}/login`, request);
  return response.data;
}

export async function refresh(): Promise<LoginResponse> {
  const response = await httpClient.post<LoginResponse>(`${BASE_PATH}/refresh`);
  return response.data;
}

export async function logout(): Promise<void> {
  await httpClient.post(`${BASE_PATH}/logout`);
}
