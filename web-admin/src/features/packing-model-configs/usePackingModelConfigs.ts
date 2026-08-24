import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { create, downloadTemplate, getAll, update, uploadTemplate } from '../../api/packingModelConfigsApi';
import type { CreatePackingModelConfigRequest, UpdatePackingModelConfigRequest } from '../../types/packingModelConfig';

const PACKING_MODEL_CONFIGS_QUERY_KEY = ['packing-model-configs'];

/** `GET /api/v1/packing-model-configs` (AC3) */
export function usePackingModelConfigs() {
  return useQuery({
    queryKey: PACKING_MODEL_CONFIGS_QUERY_KEY,
    queryFn: getAll,
  });
}

/** `POST /api/v1/packing-model-configs` (AC1) */
export function useCreatePackingModelConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreatePackingModelConfigRequest) => create(request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKING_MODEL_CONFIGS_QUERY_KEY });
    },
  });
}

/** `PUT /api/v1/packing-model-configs/{id}` (AC2) */
export function useUpdatePackingModelConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdatePackingModelConfigRequest }) => update(id, request),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKING_MODEL_CONFIGS_QUERY_KEY });
    },
  });
}

/** `POST /api/v1/packing-model-configs/{id}/template` (AC4) */
export function useUploadPackingTemplate() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: number; file: File }) => uploadTemplate(id, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: PACKING_MODEL_CONFIGS_QUERY_KEY });
    },
  });
}

/** `GET /api/v1/packing-model-configs/{id}/template` (AC5) — trả `Blob`, caller tự tạo link tải xuống (window.URL.createObjectURL). */
export function useDownloadPackingTemplate() {
  return useMutation({
    mutationFn: (id: number) => downloadTemplate(id),
  });
}
