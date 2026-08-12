import { SafetyCertificateOutlined } from '@ant-design/icons';
import { Layout, Menu, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const { Header, Sider, Content } = Layout;

/**
 * Khung layout Ant Design cơ bản (Header/Sider/Content). Menu điều hướng hiện chỉ có mục "Quản lý
 * phân quyền" (chỉ hiển thị cho `Admin` — break-glass, ADR-004) — các mục Line/Stage/WorkStation/
 * ProductionPlan/User thuộc phạm vi task sau.
 */
export function AppLayout() {
  const user = useAuthStore((state) => state.user);
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems: MenuProps['items'] = useMemo(() => {
    if (user?.userRole !== 'Admin') {
      return [];
    }
    return [
      {
        key: '/permissions',
        icon: <SafetyCertificateOutlined />,
        label: 'Quản lý phân quyền',
      },
    ];
  }, [user?.userRole]);

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
          <Menu
            mode="inline"
            selectedKeys={[location.pathname]}
            items={menuItems}
            onClick={({ key }) => navigate(key)}
          />
        </Sider>
        <Content style={{ padding: 24 }}>
          <Outlet />
        </Content>
      </Layout>
    </Layout>
  );
}
