import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { useAuthStore } from '../store/authStore';
import type { LoginResponse } from '../types/auth';

/**
 * Axios instance dùng chung cho toàn bộ module `api/`. `withCredentials: true` bắt buộc để cookie
 * HttpOnly (access_token/refresh_token) tự gửi kèm (ADR-003).
 */
export const httpClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  withCredentials: true,
});

/** CSRF token hiện tại — chỉ giữ trong bộ nhớ (module-level), không persist. */
let csrfToken: string | null = null;

export function setCsrfToken(token: string): void {
  csrfToken = token;
}

export function getCsrfToken(): string | null {
  return csrfToken;
}

const AUTH_LOGIN_PATH = '/auth/login';
const AUTH_REFRESH_PATH = '/auth/refresh';

// Request interceptor: gắn header X-CSRF-TOKEN cho mọi POST/PUT (API-Conventions.md mục 7).
httpClient.interceptors.request.use((config) => {
  const method = config.method?.toUpperCase();
  if ((method === 'POST' || method === 'PUT') && csrfToken) {
    config.headers.set('X-CSRF-TOKEN', csrfToken);
  }
  return config;
});

type RetriableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean };

// Dedup nhiều request 401 đồng thời chỉ gọi refresh 1 lần.
let refreshPromise: Promise<LoginResponse> | null = null;

function refreshAccessToken(): Promise<LoginResponse> {
  if (!refreshPromise) {
    refreshPromise = httpClient
      .post<LoginResponse>(AUTH_REFRESH_PATH)
      .then((response) => response.data)
      .finally(() => {
        refreshPromise = null;
      });
  }
  return refreshPromise;
}

// Response interceptor: 401 (không phải từ login/refresh) → refresh 1 lần → thử lại request gốc 1 lần.
// Refresh cũng 401 → clear session + điều hướng /login (API-Conventions.md mục 7, ADR-003).
httpClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined;
    const requestUrl = originalRequest?.url ?? '';
    const isAuthEndpoint = requestUrl.includes(AUTH_LOGIN_PATH) || requestUrl.includes(AUTH_REFRESH_PATH);

    if (error.response?.status === 401 && originalRequest && !isAuthEndpoint && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshed = await refreshAccessToken();
        useAuthStore.getState().setUser({
          username: refreshed.username,
          fullName: refreshed.fullName,
          userRole: refreshed.userRole,
          permissions: refreshed.permissions,
        });
        return httpClient(originalRequest);
      } catch (refreshError) {
        useAuthStore.getState().clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);
