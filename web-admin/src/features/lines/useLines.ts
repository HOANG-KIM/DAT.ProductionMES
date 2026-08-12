import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, deactivate, getAll, update } from '../../api/linesApi';
import type { CreateLineRequest, UpdateLineRequest } from '../../types/line';

const LINES_QUERY_KEY = ['lines'];

/** `GET /api/v1/lines` */
export function useLines() {
  return useQuery({
    queryKey: LINES_QUERY_KEY,
    queryFn: getAll,
  });
}

/** `POST /api/v1/lines` */
export function useCreateLine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateLineRequest) => create(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LINES_QUERY_KEY });
    },
  });
}

/** `PUT /api/v1/lines/{id}` */
export function useUpdateLine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateLineRequest }) => update(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LINES_QUERY_KEY });
    },
  });
}

/** `POST /api/v1/lines/{id}/deactivate` */
export function useDeactivateLine() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: LINES_QUERY_KEY });
    },
  });
}
