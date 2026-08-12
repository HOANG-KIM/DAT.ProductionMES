import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, deactivate, getAll, updateRole } from '../../api/usersApi';
import type { CreateUserRequest, UpdateUserRoleRequest } from '../../types/user';

const USERS_QUERY_KEY = ['users'];

/** `GET /api/v1/users` */
export function useUsers() {
  return useQuery({
    queryKey: USERS_QUERY_KEY,
    queryFn: getAll,
  });
}

/** `POST /api/v1/users` */
export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateUserRequest) => create(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: USERS_QUERY_KEY });
    },
  });
}

/** `PUT /api/v1/users/{id}/role` */
export function useUpdateUserRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateUserRoleRequest }) => updateRole(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: USERS_QUERY_KEY });
    },
  });
}

/** `POST /api/v1/users/{id}/deactivate` */
export function useDeactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: USERS_QUERY_KEY });
    },
  });
}
