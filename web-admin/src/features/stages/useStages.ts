import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, deactivate, getAll, update } from '../../api/stagesApi';
import type { CreateStageRequest, UpdateStageRequest } from '../../types/stage';

const STAGES_QUERY_KEY = ['stages'];

/** `GET /api/v1/stages` */
export function useStages() {
  return useQuery({
    queryKey: STAGES_QUERY_KEY,
    queryFn: getAll,
  });
}

/** `POST /api/v1/stages` */
export function useCreateStage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateStageRequest) => create(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: STAGES_QUERY_KEY });
    },
  });
}

/** `PUT /api/v1/stages/{id}` */
export function useUpdateStage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateStageRequest }) => update(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: STAGES_QUERY_KEY });
    },
  });
}

/** `POST /api/v1/stages/{id}/deactivate` */
export function useDeactivateStage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: STAGES_QUERY_KEY });
    },
  });
}
