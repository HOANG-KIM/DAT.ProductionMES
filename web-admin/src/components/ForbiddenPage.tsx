import { Button, Result } from 'antd';
import { useNavigate } from 'react-router-dom';

/**
 * Trang 403 — hiển thị khi user đã đăng nhập hợp lệ nhưng thiếu permission cho route hiện tại
 * (`PermissionGuard`). Khác `RouteGuard` (redirect `/login` khi chưa đăng nhập), trang này KHÔNG
 * điều hướng login vì phiên vẫn hợp lệ, chỉ thiếu quyền cho trang cụ thể.
 */
export function ForbiddenPage() {
  const navigate = useNavigate();

  return (
    <Result
      status="403"
      title="403"
      subTitle="Bạn không có quyền truy cập trang này"
      extra={
        <Button type="primary" onClick={() => navigate('/')}>
          Về trang chủ
        </Button>
      }
    />
  );
}
