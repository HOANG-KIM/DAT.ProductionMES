import {
  ApartmentOutlined,
  DesktopOutlined,
  PartitionOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
} from '@ant-design/icons';
import { Layout, Menu, Typography } from 'antd';
import type { MenuProps } from 'antd';
import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { useAuthStore } from '../store/authStore';

const { Header, Sider, Content } = Layout;

/**
 * Khung layout Ant Design cơ bản (Header/Sider/Content). Menu điều hướng lọc theo permission động
 * (ADR-004) qua `hasPermission` (Line/Stage/WorkStation), riêng "Quản lý người dùng" và "Quản lý
 * phân quyền" vẫn hardcode `Admin` (break-glass, không đi qua permission động — `UsersController`
 * cũng hardcode `[Authorize(Roles="Admin")]`). Mục ProductionPlan thuộc phạm vi task sau (không nằm
 * ở web-admin, xem ADR-002).
 */
export function AppLayout() {
  const user = useAuthStore((state) => state.user);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems: MenuProps['items'] = useMemo(() => {
    const items: MenuProps['items'] = [];

    if (hasPermission('Line.View')) {
      items.push({
        key: '/lines',
        icon: <ApartmentOutlined />,
        label: 'Quản lý Line',
      });
    }

    if (hasPermission('Stage.View')) {
      items.push({
        key: '/stages',
        icon: <PartitionOutlined />,
        label: 'Quản lý Công đoạn',
      });
    }

    if (hasPermission('WorkStation.View')) {
      items.push({
        key: '/work-stations',
        icon: <DesktopOutlined />,
        label: 'Quản lý Trạm làm việc',
      });
    }

    if (user?.userRole === 'Admin') {
      items.push({
        key: '/users',
        icon: <TeamOutlined />,
        label: 'Quản lý người dùng',
      });
    }

    if (user?.userRole === 'Admin') {
      items.push({
        key: '/permissions',
        icon: <SafetyCertificateOutlined />,
        label: 'Quản lý phân quyền',
      });
    }

    return items;
  }, [user?.userRole, hasPermission]);

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
