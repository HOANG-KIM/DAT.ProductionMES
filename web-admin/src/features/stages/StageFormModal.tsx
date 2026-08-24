import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Checkbox, Form, Input, Modal } from 'antd';
import type { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { useCreateStage, useUpdateStage } from './useStages';
import type { Stage } from '../../types/stage';

/** Đối chiếu FluentValidation `CreateStageRequestValidator`/`UpdateStageRequestValidator` (backend). */
const stageSchema = z.object({
  name: z.string().min(1, 'Tên công đoạn không được để trống').max(200, 'Tên công đoạn tối đa 200 ký tự'),
  description: z.string().max(1000, 'Mô tả tối đa 1000 ký tự').optional(),
  isPackingStage: z.boolean(),
});

type StageFormValues = z.infer<typeof stageSchema>;

interface StageFormModalProps {
  open: boolean;
  /** Stage đang sửa — `null` nghĩa là đang tạo mới. */
  editingStage: Stage | null;
  onClose: () => void;
}

/** Modal tạo mới/sửa Stage (US-02) — dùng chung 1 form cho cả 2 chế độ, phân biệt qua `editingStage`. */
export function StageFormModal({ open, editingStage, onClose }: StageFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const createMutation = useCreateStage();
  const updateMutation = useUpdateStage();
  const isSubmitting = createMutation.isPending || updateMutation.isPending;

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<StageFormValues>({
    resolver: zodResolver(stageSchema),
    defaultValues: { name: '', description: '', isPackingStage: false },
  });

  useEffect(() => {
    if (open) {
      reset({
        name: editingStage?.name ?? '',
        description: editingStage?.description ?? '',
        isPackingStage: editingStage?.isPackingStage ?? false,
      });
      setErrorMessage(null);
    }
  }, [open, editingStage, reset]);

  const onSubmit = async (values: StageFormValues) => {
    setErrorMessage(null);
    try {
      if (editingStage) {
        await updateMutation.mutateAsync({ id: editingStage.id, request: values });
      } else {
        await createMutation.mutateAsync(values);
      }
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string }>;
      setErrorMessage(axiosError.response?.data?.title ?? 'Lưu công đoạn thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title={editingStage ? 'Sửa công đoạn' : 'Thêm công đoạn'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={isSubmitting}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Tên công đoạn" validateStatus={errors.name ? 'error' : ''} help={errors.name?.message}>
          <Controller name="name" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item label="Mô tả" validateStatus={errors.description ? 'error' : ''} help={errors.description?.message}>
          <Controller
            name="description"
            control={control}
            render={({ field }) => <Input.TextArea {...field} rows={3} />}
          />
        </Form.Item>
        <Form.Item help="US-25: đánh dấu công đoạn này để bật đếm số lượng theo Quy cách đóng gói, tự động in tem thùng tại trạm — chỉ ĐÚNG 1 công đoạn nên được đánh dấu.">
          <Controller
            name="isPackingStage"
            control={control}
            render={({ field }) => (
              <Checkbox checked={field.value} onChange={(e) => field.onChange(e.target.checked)}>
                Là công đoạn "Đóng thùng"
              </Checkbox>
            )}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
