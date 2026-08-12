/**
 * Type khớp DTO Auth backend (`ProductionMES.Application/DTOs/Auth/`).
 * Xem `Documents/API-Conventions.md` mục 7 và ADR-003 để biết luồng auth đầy đủ.
 */

/** 4 role hệ thống — khớp enum `UserRole` backend, serialize dạng chuỗi (mục 10 API-Conventions.md). */
export type UserRole = 'Operator' | 'Supervisor' | 'Admin' | 'Manager';

/** Khớp `LoginRequest` (`ProductionMES.Application/DTOs/Auth/LoginRequest.cs`). */
export interface LoginRequest {
  username: string;
  password: string;
}

/**
 * Khớp `LoginResponse` (`ProductionMES.Application/DTOs/Auth/LoginResponse.cs`) — trả về từ cả
 * `POST /auth/login` lẫn `POST /auth/refresh`. KHÔNG chứa token (token nằm trong cookie HttpOnly).
 */
export interface LoginResponse {
  accessTokenExpiresAtUtc: string;
  username: string;
  fullName: string;
  userRole: UserRole;
}

/** Khớp response `GET /auth/csrf`. */
export interface CsrfResponse {
  csrfToken: string;
}

/** Thông tin user hiển thị UI, lưu ở `store/authStore.ts` — chỉ trong bộ nhớ phiên. */
export interface AuthUser {
  username: string;
  fullName: string;
  userRole: UserRole;
}
