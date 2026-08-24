import {
  ApartmentOutlined,
  AppstoreOutlined,
  BarChartOutlined,
  DesktopOutlined,
  DownOutlined,
  InboxOutlined,
  LogoutOutlined,
  PartitionOutlined,
  SafetyCertificateOutlined,
  TeamOutlined,
  UserOutlined,
} from '@ant-design/icons';
import { Dropdown, Layout, Menu, Space, Typography } from 'antd';
import type { MenuProps } from 'antd';
import type { ReactNode } from 'react';
import { useMemo } from 'react';
import { useLocation, useNavigate, Outlet } from 'react-router-dom';
import { logout } from '../api/authApi';
import { useAuthStore } from '../store/authStore';

const { Header, Sider, Content } = Layout;

/**
 * Khung layout Ant Design cơ bản (Header/Sider/Content). Menu điều hướng lọc theo permission động
 * (ADR-004) qua `hasPermission` (Line/Stage/WorkStation/Report — US-21), riêng "Quản lý người dùng" và "Quản lý
 * phân quyền" vẫn hardcode `Admin` (break-glass, không đi qua permission động — `UsersController`
 * cũng hardcode `[Authorize(Roles="Admin")]`). Mục ProductionPlan thuộc phạm vi task sau (không nằm
 * ở web-admin, xem ADR-002). Menu gom theo nhóm (Danh mục/Báo cáo/Quản trị hệ thống) dạng SubMenu
 * thu/phóng được (không phải `type: 'group'` cố định) — nhóm nào không còn mục con nào (do lọc quyền)
 * thì bị loại hẳn khỏi Sider, không hiện label trơ trọi.
 */
export function AppLayout() {
  const user = useAuthStore((state) => state.user);
  const hasPermission = useAuthStore((state) => state.hasPermission);
  const navigate = useNavigate();
  const location = useLocation();

  const menuItems: MenuProps['items'] = useMemo(() => {
    const catalogItems: MenuProps['items'] = [];

    if (hasPermission('Line.View')) {
      catalogItems.push({
        key: '/lines',
        icon: <ApartmentOutlined />,
        label: 'Quản lý Line',
      });
    }

    if (hasPermission('Stage.View')) {
      catalogItems.push({
        key: '/stages',
        icon: <PartitionOutlined />,
        label: 'Quản lý Công đoạn',
      });
    }

    if (hasPermission('WorkStation.View')) {
      catalogItems.push({
        key: '/work-stations',
        icon: <DesktopOutlined />,
        label: 'Quản lý Trạm làm việc',
      });
    }

    if (hasPermission('PackingModelConfig.View')) {
      catalogItems.push({
        key: '/packing-model-configs',
        icon: <InboxOutlined />,
        label: 'Cấu hình đóng gói theo Model',
      });
    }

    const reportItems: MenuProps['items'] = [];

    if (hasPermission('Report.View')) {
      reportItems.push({
        key: '/reports/production-summary',
        icon: <BarChartOutlined />,
        label: 'Báo cáo theo Lot',
      });
    }

    const adminItems: MenuProps['items'] = [];

    if (user?.userRole === 'Admin') {
      adminItems.push({
        key: '/users',
        icon: <TeamOutlined />,
        label: 'Quản lý người dùng',
      });
    }

    if (user?.userRole === 'Admin') {
      adminItems.push({
        key: '/permissions',
        icon: <SafetyCertificateOutlined />,
        label: 'Quản lý phân quyền',
      });
    }

    const groups: { key: string; label: string; icon: ReactNode; items: MenuProps['items'] }[] = [
      { key: 'group-catalog', label: 'Danh mục', icon: <AppstoreOutlined />, items: catalogItems },
      { key: 'group-reports', label: 'Báo cáo', icon: <BarChartOutlined />, items: reportItems },
      { key: 'group-admin', label: 'Quản trị hệ thống', icon: <SafetyCertificateOutlined />, items: adminItems },
    ];

    const items: MenuProps['items'] = [];
    for (const group of groups) {
      if (group.items && group.items.length > 0) {
        items.push({
          key: group.key,
          icon: group.icon,
          label: group.label,
          children: group.items,
        });
      }
    }

    return items;
  }, [user?.userRole, hasPermission]);

  const handleLogout = async () => {
    try {
      await logout();
    } catch {
      // Vẫn tiếp tục dọn session phía client dù API logout lỗi — tránh người dùng kẹt lại không đăng xuất được.
    } finally {
      useAuthStore.getState().clear();
      navigate('/login');
    }
  };

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      label: 'Đăng xuất',
      onClick: () => void handleLogout(),
    },
  ];

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Typography.Title level={4} style={{ color: '#fff', margin: 0 }}>
          ĐẠI Á THÀNH
        </Typography.Title>
        {user && (
          <Dropdown menu={{ items: userMenuItems }} trigger={['click']}>
            <Typography.Text style={{ color: '#fff', cursor: 'pointer' }}>
              <Space size={4}>
                <UserOutlined />
                {user.fullName} ({user.userRole})
                <DownOutlined style={{ fontSize: 10 }} />
              </Space>
            </Typography.Text>
          </Dropdown>
        )}
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
