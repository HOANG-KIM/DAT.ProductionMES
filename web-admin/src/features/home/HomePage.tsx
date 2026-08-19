import { Typography } from 'antd';
import { useDocumentTitle } from '../../hooks/useDocumentTitle';

/** Trang chủ placeholder — các trang CRUD Line/Stage/WorkStation/... thuộc phạm vi task sau. */
export function HomePage() {
  useDocumentTitle('Trang chủ');

  return (
    <div>
      <Typography.Title level={3}>Trang chủ</Typography.Title>
      <Typography.Paragraph>Khung ứng dụng quản trị DAT.ProductionMES.</Typography.Paragraph>
    </div>
  );
}
