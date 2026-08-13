import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Divider, Form, Input, Modal } from 'antd';
import type { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { BreakWindowSection } from './BreakWindowSection';
import { useCreateLine, useUpdateLine } from './useLines';
import { useAuthStore } from '../../store/authStore';
import type { Line } from '../../types/line';

/** Đối chiếu FluentValidation `CreateLineRequestValidator`/`UpdateLineRequestValidator` (backend). */
const lineSchema = z.object({
  name: z.string().min(1, 'Tên Line không được để trống').max(200, 'Tên Line tối đa 200 ký tự'),
  description: z.string().max(1000, 'Mô tả tối đa 1000 ký tự').optional(),
});

type LineFormValues = z.infer<typeof lineSchema>;

interface LineFormModalProps {
  open: boolean;
  /** Line đang sửa — `null` nghĩa là đang tạo mới. */
  editingLine: Line | null;
  onClose: () => void;
}

/** Modal tạo mới/sửa Line (AC1/AC2) — dùng chung 1 form cho cả 2 chế độ, phân biệt qua `editingLine`. */
export function LineFormModal({ open, editingLine, onClose }: LineFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const createMutation = useCreateLine();
  const updateMutation = useUpdateLine();
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<LineFormValues>({
    resolver: zodResolver(lineSchema),
    defaultValues: { name: '', description: '' },
  });

  useEffect(() => {
    if (open) {
      reset({ name: editingLine?.name ?? '', description: editingLine?.description ?? '' });
      setErrorMessage(null);
    }
  }, [open, editingLine, reset]);

  const onSubmit = async (values: LineFormValues) => {
    setErrorMessage(null);
    try {
      if (editingLine) {
        await updateMutation.mutateAsync({ id: editingLine.id, request: values });
      } else {
        await createMutation.mutateAsync(values);
      }
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string }>;
      setErrorMessage(axiosError.response?.data?.title ?? 'Lưu Line thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title={editingLine ? 'Sửa Line' : 'Thêm Line'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Tên Line" validateStatus={errors.name ? 'error' : ''} help={errors.name?.message}>
          <Controller name="name" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item label="Mô tả" validateStatus={errors.description ? 'error' : ''} help={errors.description?.message}>
          <Controller
            name="description"
            control={control}
            render={({ field }) => <Input.TextArea {...field} rows={3} />}
          />
        </Form.Item>
      </Form>

      {/* US-01a: khung giờ nghỉ chỉ cấu hình được khi Line đã tồn tại (cần lineId) — không hiển thị lúc tạo mới. */}
      {editingLine && hasPermission('BreakWindow.View') && (
        <>
          <Divider />
          <BreakWindowSection lineId={editingLine.id} />
        </>
      )}
    </Modal>
  );
}
