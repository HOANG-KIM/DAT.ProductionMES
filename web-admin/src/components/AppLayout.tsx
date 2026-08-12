import { Layout, Typography } from 'antd';
import { Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const { Header, Sider, Content } = Layout;

/**
 * Khung layout Ant Design cơ bản (Header/Sider/Content) — chưa dựng menu điều hướng thật
 * (US-01→06 chưa có trang CRUD nào ở web-admin, đây chỉ là khung cho các task sau).
 */
export function AppLayout() {
  const user = useAuthStore((state) => state.user);

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ color: '#fff', margin: 0 }}>
          DAT.ProductionMES — Quản trị
        </Typography.Title>
        {user && <Typography.Text style={{ color: '#fff' }}>{user.fullName} ({user.userRole})</Typography.Text>}
      </Header>
      <Layout>
        <Sider width={220} theme="light">
          {/* TODO: menu điều hướng Line/Stage/WorkStation/ProductionPlan/User — chưa thuộc phạm vi scaffold này */}
        </Sider>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
