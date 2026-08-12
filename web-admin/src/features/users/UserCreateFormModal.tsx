import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Form, Input, Modal, Select } from 'antd';
import type { AxiosError } from 'axios';
import { useEffect, useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { z } from 'zod';
import { ROLE_OPTIONS } from './roleLabels';
import { useCreateUser } from './useUsers';

/**
 * Đối chiếu FluentValidation `CreateUserRequestValidator` (backend) — không có rule độ phức tạp mật
 * khẩu nào khác ngoài required + max length (không tự thêm rule chữ hoa/số).
 */
const userSchema = z.object({
  username: z.string().min(1, 'Tên đăng nhập không được để trống').max(100, 'Tên đăng nhập tối đa 100 ký tự'),
  password: z.string().min(1, 'Mật khẩu không được để trống').max(200, 'Mật khẩu tối đa 200 ký tự'),
  fullName: z.string().min(1, 'Họ tên không được để trống').max(200, 'Họ tên tối đa 200 ký tự'),
  userRole: z.enum(['Operator', 'Supervisor', 'Admin', 'Manager'], { error: 'Vai trò không hợp lệ' }),
});

type UserFormValues = z.infer<typeof userSchema>;

const EMPTY_VALUES: UserFormValues = {
  username: '',
  password: '',
  fullName: '',
  userRole: 'Operator',
};

interface UserCreateFormModalProps {
  open: boolean;
  onClose: () => void;
}

/**
 * Modal tạo mới 1 tài khoản (US-22/AC1). Khác Line/Stage/WorkStation: API hiện có chỉ cho phép sửa
 * vai trò (`PUT /users/{id}/role`), không sửa được Username/FullName/Password — nên tách riêng modal
 * này (chỉ tạo mới) và `UserRoleFormModal` (chỉ sửa vai trò), không dùng chung 1 form như Line.
 */
export function UserCreateFormModal({ open, onClose }: UserCreateFormModalProps) {
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const createMutation = useCreateUser();

  const {
    control,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<UserFormValues>({
    resolver: zodResolver(userSchema),
    defaultValues: EMPTY_VALUES,
  });

  useEffect(() => {
    if (open) {
      reset(EMPTY_VALUES);
      setErrorMessage(null);
    }
  }, [open, reset]);

  const onSubmit = async (values: UserFormValues) => {
    setErrorMessage(null);
    try {
      await createMutation.mutateAsync(values);
      onClose();
    } catch (error) {
      const axiosError = error as AxiosError<{ title?: string }>;
      setErrorMessage(axiosError.response?.data?.title ?? 'Tạo tài khoản thất bại, vui lòng thử lại');
    }
  };

  return (
    <Modal
      title="Thêm người dùng"
      open={open}
      onOk={handleSubmit(onSubmit)}
      onCancel={onClose}
      confirmLoading={createMutation.isPending}
      destroyOnHidden
    >
      {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
      <Form layout="vertical">
        <Form.Item label="Tên đăng nhập" validateStatus={errors.username ? 'error' : ''} help={errors.username?.message}>
          <Controller name="username" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item label="Mật khẩu" validateStatus={errors.password ? 'error' : ''} help={errors.password?.message}>
          <Controller
            name="password"
            control={control}
            render={({ field }) => <Input.Password {...field} />}
          />
        </Form.Item>
        <Form.Item label="Họ tên" validateStatus={errors.fullName ? 'error' : ''} help={errors.fullName?.message}>
          <Controller name="fullName" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
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
