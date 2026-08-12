import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Card, Form, Input, Typography } from 'antd';
import { type AxiosError } from 'axios';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { login } from '../../api/authApi';
import { useAuthStore } from '../../store/authStore';

const loginSchema = z.object({
  username: z.string().min(1, 'Vui lòng nhập tên đăng nhập'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
});

type LoginFormValues = z.infer<typeof loginSchema>;

/** Màn hình đăng nhập — submit gọi `authApi.login`, thành công thì lưu session và điều hướng trang chủ. */
export function LoginPage() {
  const navigate = useNavigate();
  const setUser = useAuthStore((state) => state.setUser);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginSchema),
    defaultValues: { username: '', password: '' },
  });

  const onSubmit = async (values: LoginFormValues) => {
    setErrorMessage(null);
    setIsSubmitting(true);
    try {
      const response = await login(values);
      setUser({
        username: response.username,
        fullName: response.fullName,
        userRole: response.userRole,
        permissions: response.permissions,
      });
      navigate('/', { replace: true });
    } catch (error) {
      const axiosError = error as AxiosError;
      if (axiosError.response?.status === 401) {
        setErrorMessage('Sai tên đăng nhập hoặc mật khẩu');
      } else {
        setErrorMessage('Đăng nhập thất bại, vui lòng thử lại');
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        minHeight: '100vh',
        background: '#f0f2f5',
      }}
    >
      <Card style={{ width: 360 }}>
        <Typography.Title level={3} style={{ textAlign: 'center' }}>
          DAT.ProductionMES
        </Typography.Title>
        <Typography.Paragraph style={{ textAlign: 'center' }}>Đăng nhập quản trị</Typography.Paragraph>
        {errorMessage && <Alert type="error" message={errorMessage} style={{ marginBottom: 16 }} showIcon />}
        <Form layout="vertical" onFinish={handleSubmit(onSubmit)}>
          <Form.Item label="Tên đăng nhập" validateStatus={errors.username ? 'error' : ''} help={errors.username?.message}>
            <Controller
              name="username"
              control={control}
              render={({ field }) => <Input {...field} autoComplete="username" />}
            />
          </Form.Item>
          <Form.Item label="Mật khẩu" validateStatus={errors.password ? 'error' : ''} help={errors.password?.message}>
            <Controller
              name="password"
              control={control}
              render={({ field }) => <Input.Password {...field} autoComplete="current-password" />}
            />
          </Form.Item>
          <Form.Item>
            <Button type="primary" htmlType="submit" block loading={isSubmitting}>
              Đăng nhập
            </Button>
          </Form.Item>
        </Form>
      </Card>
    </div>
  );
}
