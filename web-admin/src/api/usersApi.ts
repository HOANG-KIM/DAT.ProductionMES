import { httpClient } from './httpClient';
import type { CreateUserRequest, UpdateUserRoleRequest, User } from '../types/user';

const BASE_PATH = '/api/v1/users';

/** `GET /api/v1/users` — toàn bộ tài khoản (kể cả đã vô hiệu hóa), không phân trang (danh mục nhỏ). */
export async function getAll(): Promise<User[]> {
  const response = await httpClient.get<User[]>(BASE_PATH);
  return response.data;
}

/** `POST /api/v1/users` */
export async function create(request: CreateUserRequest): Promise<User> {
  const response = await httpClient.post<User>(BASE_PATH, request);
  return response.data;
}

/** `PUT /api/v1/users/{id}/role` — chỉ sửa vai trò, không sửa các field khác. */
export async function updateRole(id: number, request: UpdateUserRoleRequest): Promise<User> {
  const response = await httpClient.put<User>(`${BASE_PATH}/${id}/role`, request);
  return response.data;
}

/** `POST /api/v1/users/{id}/deactivate` — soft-delete, trả `204 No Content`. */
export async function deactivate(id: number): Promise<void> {
  await httpClient.post(`${BASE_PATH}/${id}/deactivate`);
}
