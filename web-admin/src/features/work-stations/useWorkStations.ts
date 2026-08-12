import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, deactivate, getAll, update } from '../../api/workStationsApi';
import type { CreateWorkStationRequest, UpdateWorkStationRequest } from '../../types/workStation';

const WORK_STATIONS_QUERY_KEY = ['work-stations'];

/** `GET /api/v1/work-stations` */
export function useWorkStations() {
  return useQuery({
    queryKey: WORK_STATIONS_QUERY_KEY,
    queryFn: getAll,
  });
}

/** `POST /api/v1/work-stations` */
export function useCreateWorkStation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateWorkStationRequest) => create(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: WORK_STATIONS_QUERY_KEY });
    },
  });
}

/** `PUT /api/v1/work-stations/{id}` */
export function useUpdateWorkStation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateWorkStationRequest }) => update(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: WORK_STATIONS_QUERY_KEY });
    },
  });
}

/** `POST /api/v1/work-stations/{id}/deactivate` */
export function useDeactivateWorkStation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: number) => deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: WORK_STATIONS_QUERY_KEY });
    },
  });
}
