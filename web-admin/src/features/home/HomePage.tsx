import { Typography } from 'antd';

/** Trang chủ placeholder — các trang CRUD Line/Stage/WorkStation/... thuộc phạm vi task sau. */
export function HomePage() {
  return (
    <div>
      <Typography.Title level={3}>Trang chủ</Typography.Title>
      <Typography.Paragraph>Khung ứng dụng quản trị DAT.ProductionMES.</Typography.Paragraph>
    </div>
  );
}
