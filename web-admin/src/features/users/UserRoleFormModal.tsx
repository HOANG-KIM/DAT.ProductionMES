import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Form, Modal, Select } from 'antd';
import type { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { ROLE_OPTIONS } from './roleLabels';
import { useUpdateUserRole } from './useUsers';
import type { User } from '../../types/user';

/** Đối chiếu FluentValidation `UpdateUserRoleRequestValidator` (backend). */
const userRoleSchema = z.object({
  userRole: z.enum(['Operator', 'Supervisor', 'Admin', 'Manager'], { error: 'Vai trò không hợp lệ' }),
});

type UserRoleFormValues = z.infer<typeof userRoleSchema>;

interface UserRoleFormModalProps {
  open: boolean;
  /** Tài khoản đang sửa vai trò — bắt buộc (modal chỉ mở khi có tài khoản, không có chế độ "tạo mới"). */
  editingUser: User | null;
  onClose: () => void;
}

/**
 * Modal sửa vai trò 1 tài khoản (US-22/AC1) — `PUT /users/{id}/role` chỉ nhận `userRole`, nên form
 * chỉ có đúng 1 field (khác Line/Stage/WorkStation sửa full form), tách riêng khỏi `UserCreateFormModal`.
 */
export function UserRoleFormModal({ open, editingUser, onClose }: UserRoleFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const updateRoleMutation = useUpdateUserRole();

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UserRoleFormValues>({
    resolver: zodResolver(userRoleSchema),
    defaultValues: { userRole: 'Operator' },
  });

  useEffect(() => {
    if (open) {
      reset({ userRole: editingUser?.userRole ?? 'Operator' });
      setErrorMessage(null);
    }
  }, [open, editingUser, reset]);

  const onSubmit = async (values: UserRoleFormValues) => {
    if (!editingUser) {
      return;
    }
    setErrorMessage(null);
    try {
      await updateRoleMutation.mutateAsync({ id: editingUser.id, request: values });
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string }>;
      setErrorMessage(axiosError.response?.data?.title ?? 'Cập nhật vai trò thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title={editingUser ? `Sửa vai trò — ${editingUser.username}` : 'Sửa vai trò'}
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={updateRoleMutation.isPending}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Vai trò" validateStatus={errors.userRole ? 'error' : ''} help={errors.userRole?.message}>
          <Controller
            name="userRole"
            control={control}
            render={({ field: { onChange, onBlur, value } }) => (
              <Select value={value} onChange={onChange} onBlur={onBlur} options={ROLE_OPTIONS} />
            )}
          />
        </Form.Item>
      </Form>
    </Modal>
  );
}
