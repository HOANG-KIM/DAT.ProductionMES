import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Form, Input, Modal, TimePicker } from 'antd';
import type { AxiosError } from 'axios';
import dayjs, { type Dayjs } from 'dayjs';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCreateBreakWindow, useUpdateBreakWindow } from './useBreakWindows';
import type { BreakWindow } from '../../types/breakWindow';

const TIME_FORMAT = 'HH:mm';

/** Đối chiếu FluentValidation `CreateBreakWindowRequestValidator`/`UpdateBreakWindowRequestValidator` (backend) —
 * phần "không chồng lấn" (AC5) chỉ validate được ở server (cần dữ liệu các khung giờ nghỉ khác), hiển thị lỗi
 * trả về từ backend (409) khi submit. */
const breakWindowSchema = z
  .object({
    startTime: z.custom<Dayjs | null>((v) => dayjs.isDayjs(v), 'Giờ bắt đầu không được để trống'),
    endTime: z.custom<Dayjs | null>((v) => dayjs.isDayjs(v), 'Giờ kết thúc không được để trống'),
    note: z.string().max(500, 'Ghi chú tối đa 500 ký tự').optional(),
  })
  .refine((data) => !dayjs.isDayjs(data.startTime) || !dayjs.isDayjs(data.endTime) || data.endTime.isAfter(data.startTime), {
    message: 'Giờ kết thúc phải lớn hơn giờ bắt đầu',
    path: ['endTime'],
  });

type BreakWindowFormValues = z.infer<typeof breakWindowSchema>;

interface BreakWindowFormModalProps {
  open: boolean;
  lineId: number;
  /** Khung giờ nghỉ đang sửa — `null` nghĩa là đang tạo mới (AC1/AC3). */
  editingBreakWindow: BreakWindow | null;
  onClose: () => void;
}

/** Modal tạo mới/sửa khung giờ nghỉ của Line (US-01a AC1/AC3). */
export function BreakWindowFormModal({ open, lineId, editingBreakWindow, onClose }: BreakWindowFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const createMutation = useCreateBreakWindow(lineId);
  const updateMutation = useUpdateBreakWindow(lineId);
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<BreakWindowFormValues>({
    resolver: zodResolver(breakWindowSchema),
    defaultValues: { startTime: null, endTime: null, note: '' },
  });

  useEffect(() => {
    if (open) {
      reset({
        startTime: editingBreakWindow ? dayjs(editingBreakWindow.startTime, 'HH:mm:ss') : null,
        endTime: editingBreakWindow ? dayjs(editingBreakWindow.endTime, 'HH:mm:ss') : null,
        note: editingBreakWindow?.note ?? '',
      });
      setErrorMessage(null);
    }
  }, [open, editingBreakWindow, reset]);

  const onSubmit = async (values: BreakWindowFormValues) => {
    setErrorMessage(null);
    const request = {
      startTime: (values.startTime as Dayjs).format('HH:mm:ss'),
      endTime: (values.endTime as Dayjs).format('HH:mm:ss'),
      note: values.note || null,
    };
    try {
      if (editingBreakWindow) {
        await updateMutation.mutateAsync({ id: editingBreakWindow.id, request });
      } else {
        await createMutation.mutateAsync(request);
      }
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ detail?: string; title?: string }>;
      setErrorMessage(axiosError.response?.data?.detail ?? axiosError.response?.data?.title ?? 'Lưu khung giờ nghỉ thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title={editingBreakWindow ? 'Sửa khung giờ nghỉ' : 'Thêm khung giờ nghỉ'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Giờ bắt đầu" validateStatus={errors.startTime ? 'error' : ''} help={errors.startTime?.message}>
          <Controller
            name="startTime"
            control={control}
            render={({ field }) => <TimePicker {...field} format={TIME_FORMAT} style={{ width: '100%' }} />}
          />
        </Form.Item>
        <Form.Item label="Giờ kết thúc" validateStatus={errors.endTime ? 'error' : ''} help={errors.endTime?.message}>
          <Controller
            name="endTime"
            control={control}
            render={({ field }) => <TimePicker {...field} format={TIME_FORMAT} style={{ width: '100%' }} />}
          />
        </Form.Item>
        <Form.Item label="Ghi chú" validateStatus={errors.note ? 'error' : ''} help={errors.note?.message}>
          <Controller
            name="note"
            control={control}
            render={({ field }) => <Input {...field} placeholder="Vd: Nghỉ trưa, Nghỉ giữa giờ" />}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
