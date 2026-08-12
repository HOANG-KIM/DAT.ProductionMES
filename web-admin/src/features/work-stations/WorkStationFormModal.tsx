import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Form, Input, InputNumber, Modal, Select, Switch } from 'antd';
import type { AxiosError } from 'axios';
import { useEffect, useMemo, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCreateWorkStation, useUpdateWorkStation } from './useWorkStations';
import { useLines } from '../lines/useLines';
import { useStages } from '../stages/useStages';
import type { WorkStation } from '../../types/workStation';

/**
 * Đối chiếu FluentValidation `CreateWorkStationRequestValidator`/`UpdateWorkStationRequestValidator`
 * (backend, US-04/AC2/AC3): khi `useArduino = true`, `comPort`/`baudRate`/`commandProtocol` bắt buộc;
 * khi `false`, không bắt buộc. Dùng `superRefine` vì rule phụ thuộc lẫn nhau giữa các field.
 */
const workStationSchema = z
  .object({
    name: z.string().min(1, 'Tên trạm không được để trống').max(200, 'Tên trạm tối đa 200 ký tự'),
    lineId: z.number().int().positive('Phải chọn 1 Line hợp lệ'),
    stageId: z.number().int().positive('Phải chọn 1 công đoạn hợp lệ'),
    useArduino: z.boolean(),
    comPort: z.string().max(50, 'Cổng COM tối đa 50 ký tự').optional().nullable(),
    baudRate: z.number().int().optional().nullable(),
    commandProtocol: z.string().max(200, 'Giao thức lệnh tối đa 200 ký tự').optional().nullable(),
  })
  .superRefine((values, ctx) => {
    if (!values.useArduino) {
      return;
    }
    if (!values.comPort || values.comPort.trim() === '') {
      ctx.addIssue({
        code: 'custom',
        path: ['comPort'],
        message: 'Cổng COM không được để trống khi trạm sử dụng Arduino.',
      });
    }
    if (values.baudRate == null || values.baudRate <= 0) {
      ctx.addIssue({ code: 'custom', path: ['baudRate'], message: 'Baud rate phải lớn hơn 0.' });
    }
    if (!values.commandProtocol || values.commandProtocol.trim() === '') {
      ctx.addIssue({
        code: 'custom',
        path: ['commandProtocol'],
        message: 'Giao thức lệnh không được để trống khi trạm sử dụng Arduino.',
      });
    }
  });

type WorkStationFormValues = z.infer<typeof workStationSchema>;

const EMPTY_VALUES: WorkStationFormValues = {
  name: '',
  lineId: 0,
  stageId: 0,
  useArduino: false,
  comPort: '',
  baudRate: null,
  commandProtocol: '',
};

interface WorkStationFormModalProps {
  open: boolean;
  /** WorkStation đang sửa — `null` nghĩa là đang tạo mới. */
  editingWorkStation: WorkStation | null;
  onClose: () => void;
}

/** Modal tạo mới/sửa WorkStation (US-04) — dùng chung 1 form cho cả 2 chế độ, phân biệt qua `editingWorkStation`. */
export function WorkStationFormModal({ open, editingWorkStation, onClose }: WorkStationFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const linesQuery = useLines();
  const stagesQuery = useStages();
  const createMutation = useCreateWorkStation();
  const updateMutation = useUpdateWorkStation();
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const {
    control,
    handleSubmit,
    reset,
    watch,
    formState: { errors },
  } = useForm<WorkStationFormValues>({
    resolver: zodResolver(workStationSchema),
    defaultValues: EMPTY_VALUES,
  });

  const useArduino = watch('useArduino');

  useEffect(() => {
    if (open) {
      reset(
        editingWorkStation
          ? {
              name: editingWorkStation.name,
              lineId: editingWorkStation.lineId,
              stageId: editingWorkStation.stageId,
              useArduino: editingWorkStation.useArduino,
              comPort: editingWorkStation.comPort ?? '',
              baudRate: editingWorkStation.baudRate ?? null,
              commandProtocol: editingWorkStation.commandProtocol ?? '',
            }
          : EMPTY_VALUES,
      );
      setErrorMessage(null);
    }
  }, [open, editingWorkStation, reset]);

  // Line/Stage chỉ hiện lựa chọn đang active — riêng giá trị hiện tại của WorkStation đang sửa vẫn
  // phải hiển thị đúng dù Line/Stage đó đã bị vô hiệu hóa sau khi gán (thêm lại vào option list nếu thiếu).
  const lineOptions = useMemo(() => {
    const lines = linesQuery.data ?? [];
    const active = lines.filter((line) => line.isActive);
    if (editingWorkStation && !active.some((line) => line.id === editingWorkStation.lineId)) {
      const current = lines.find((line) => line.id === editingWorkStation.lineId);
      if (current) {
        active.push(current);
      }
    }
    return active.map((line) => ({ value: line.id, label: line.name }));
  }, [linesQuery.data, editingWorkStation]);

  const stageOptions = useMemo(() => {
    const stages = stagesQuery.data ?? [];
    const active = stages.filter((stage) => stage.isActive);
    if (editingWorkStation && !active.some((stage) => stage.id === editingWorkStation.stageId)) {
      const current = stages.find((stage) => stage.id === editingWorkStation.stageId);
      if (current) {
        active.push(current);
      }
    }
    return active.map((stage) => ({ value: stage.id, label: stage.name }));
  }, [stagesQuery.data, editingWorkStation]);

  const onSubmit = async (values: WorkStationFormValues) => {
    setErrorMessage(null);
    const request = {
      name: values.name,
      lineId: values.lineId,
      stageId: values.stageId,
      useArduino: values.useArduino,
      comPort: values.useArduino ? values.comPort : null,
      baudRate: values.useArduino ? values.baudRate : null,
      commandProtocol: values.useArduino ? values.commandProtocol : null,
    };
    try {
      if (editingWorkStation) {
        await updateMutation.mutateAsync({ id: editingWorkStation.id, request });
      } else {
        await createMutation.mutateAsync(request);
      }
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string }>;
      setErrorMessage(axiosError.response?.data?.title ?? 'Lưu trạm làm việc thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title={editingWorkStation ? 'Sửa trạm làm việc' : 'Thêm trạm làm việc'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Tên trạm" validateStatus={errors.name ? 'error' : ''} help={errors.name?.message}>
          <Controller name="name" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item label="Line" validateStatus={errors.lineId ? 'error' : ''} help={errors.lineId?.message}>
          <Controller
            name="lineId"
            control={control}
            render={({ field: { onChange, onBlur, value } }) => (
              <Select
                value={value || undefined}
                onChange={onChange}
                onBlur={onBlur}
                options={lineOptions}
                loading={linesQuery.isLoading}
                placeholder="Chọn Line"
              />
            )}
          />
        </Form.Item>
        <Form.Item label="Công đoạn" validateStatus={errors.stageId ? 'error' : ''} help={errors.stageId?.message}>
          <Controller
            name="stageId"
            control={control}
            render={({ field: { onChange, onBlur, value } }) => (
              <Select
                value={value || undefined}
                onChange={onChange}
                onBlur={onBlur}
                options={stageOptions}
                loading={stagesQuery.isLoading}
                placeholder="Chọn công đoạn"
              />
            )}
          />
        </Form.Item>
        <Form.Item label="Sử dụng Arduino">
          <Controller
            name="useArduino"
            control={control}
            render={({ field: { value, onChange } }) => <Switch checked={value} onChange={onChange} />}
          />
        </Form.Item>
        {useArduino && (
          <>
            <Form.Item label="Cổng COM" validateStatus={errors.comPort ? 'error' : ''} help={errors.comPort?.message}>
              <Controller
                name="comPort"
                control={control}
                render={({ field }) => <Input {...field} value={field.value ?? ''} />}
              />
            </Form.Item>
            <Form.Item label="Baud rate" validateStatus={errors.baudRate ? 'error' : ''} help={errors.baudRate?.message}>
              <Controller
                name="baudRate"
                control={control}
                render={({ field: { onChange, onBlur, value, name } }) => (
                  <InputNumber
                    name={name}
                    value={value ?? undefined}
                    onChange={(newValue) => onChange(newValue ?? null)}
                    onBlur={onBlur}
                    style={{ width: '100%' }}
                  />
                )}
              />
            </Form.Item>
            <Form.Item
              label="Giao thức lệnh"
              validateStatus={errors.commandProtocol ? 'error' : ''}
              help={errors.commandProtocol?.message}
            >
              <Controller
                name="commandProtocol"
                control={control}
                render={({ field }) => <Input {...field} value={field.value ?? ''} />}
              />
            </Form.Item>
          </>
        )}
      </Form>
    </Modal>
  );
}
