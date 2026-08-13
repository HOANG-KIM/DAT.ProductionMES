import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getCurrent, issue, reissue, revoke } from '../../api/stationApiKeysApi';

const stationApiKeyQueryKey = (workStationId: number) => ['station-api-key', workStationId];

/** `GET /api/v1/work-stations/{id}/api-key` */
export function useStationApiKey(workStationId: number | null) {
  return useQuery({
    queryKey: stationApiKeyQueryKey(workStationId ?? 0),
    queryFn: () => getCurrent(workStationId!),
    enabled: workStationId !== null,
  });
}

/** `POST /api/v1/work-stations/{id}/api-key` */
export function useIssueStationApiKey(workStationId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => issue(workStationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: stationApiKeyQueryKey(workStationId) });
    },
  });
}

/** `POST /api/v1/work-stations/{id}/api-key/revoke` */
export function useRevokeStationApiKey(workStationId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => revoke(workStationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: stationApiKeyQueryKey(workStationId) });
    },
  });
}

/** `POST /api/v1/work-stations/{id}/api-key/reissue` */
export function useReissueStationApiKey(workStationId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: () => reissue(workStationId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: stationApiKeyQueryKey(workStationId) });
    },
  });
}
