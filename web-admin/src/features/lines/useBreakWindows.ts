import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, getAll, remove, update } from '../../api/breakWindowsApi';
import type { CreateBreakWindowRequest, UpdateBreakWindowRequest } from '../../types/breakWindow';

const breakWindowsQueryKey = (lineId: number) => ['break-windows', lineId];

/** `GET /api/v1/lines/{lineId}/break-windows` — chỉ chạy khi có `lineId` hợp lệ (Line đang sửa). */
export function useBreakWindows(lineId: number | null) {
  return useQuery({
    queryKey: breakWindowsQueryKey(lineId ?? 0),
    queryFn: () => getAll(lineId!),
    enabled: lineId !== null,
  });
}

/** `POST /api/v1/lines/{lineId}/break-windows` */
export function useCreateBreakWindow(lineId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateBreakWindowRequest) => create(lineId, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: breakWindowsQueryKey(lineId) });
    },
  });
}

/** `PUT /api/v1/lines/{lineId}/break-windows/{id}` */
export function useUpdateBreakWindow(lineId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateBreakWindowRequest }) => update(lineId, id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: breakWindowsQueryKey(lineId) });
    },
  });
}

/** `DELETE /api/v1/lines/{lineId}/break-windows/{id}` */
export function useDeleteBreakWindow(lineId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => remove(lineId, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: breakWindowsQueryKey(lineId) });
    },
  });
}
