import { Alert, Button, message, Modal, Spin, Tag, Typography } from 'antd';
import dayjs from 'dayjs';
import { useState } from 'react';
import { StationApiKeyRevealModal } from './StationApiKeyRevealModal';
import { useIssueStationApiKey, useReissueStationApiKey, useRevokeStationApiKey, useStationApiKey } from './useStationApiKey';
import type { IssuedStationApiKey } from '../../types/stationApiKey';

interface StationApiKeySectionProps {
  workStationId: number;
}

const DATE_FORMAT = 'DD/MM/YYYY HH:mm';

/**
 * Quản lý API Key của 1 trạm (US-04a) — nhúng trong màn hình sửa trạm (`WorkStationFormModal`), vì hệ thống
 * hiện chưa có màn hình "chi tiết trạm" riêng. Hiển thị trạng thái Active/Revoked + ngày cấp cho key hiện tại
 * (AC2); giá trị thô chỉ hiện đúng 1 lần ngay sau khi cấp/cấp lại qua `StationApiKeyRevealModal` (AC1/AC4).
 */
export function StationApiKeySection({ workStationId }: StationApiKeySectionProps) {
  const currentKeyQuery = useStationApiKey(workStationId);
  const issueMutation = useIssueStationApiKey(workStationId);
  const revokeMutation = useRevokeStationApiKey(workStationId);
  const reissueMutation = useReissueStationApiKey(workStationId);

  const [revealedKey, setRevealedKey] = useState<IssuedStationApiKey | null>(null);

  const handleIssue = async () => {
    try {
      const issued = await issueMutation.mutateAsync();
      setRevealedKey(issued);
    } catch {
      void message.error('Cấp API Key thất bại, vui lòng thử lại');
    }
  };

  const handleReissue = () => {
    Modal.confirm({
      title: 'Cấp lại API Key cho trạm này?',
      content: 'Key hiện tại sẽ chuyển sang trạng thái Revoked ngay lập tức và không dùng được nữa.',
      okText: 'Cấp lại',
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          const issued = await reissueMutation.mutateAsync();
          setRevealedKey(issued);
        } catch {
          void message.error('Cấp lại API Key thất bại, vui lòng thử lại');
        }
      },
    });
  };

  const handleRevoke = () => {
    Modal.confirm({
      title: 'Thu hồi API Key của trạm này?',
      content: 'Mọi request dùng key này sẽ bị từ chối ngay từ lần gọi kế tiếp.',
      okText: 'Thu hồi',
      okButtonProps: { danger: true },
      cancelText: 'Hủy',
      onOk: async () => {
        try {
          await revokeMutation.mutateAsync();
          void message.success('Đã thu hồi API Key');
        } catch {
          void message.error('Thu hồi API Key thất bại, vui lòng thử lại');
        }
      },
    });
  };

  if (currentKeyQuery.isLoading) {
    return <Spin size="small" />;
  }

  if (currentKeyQuery.isError) {
    return <Alert type="error" showIcon message="Không tải được trạng thái API Key" />;
  }

  const currentKey = currentKeyQuery.data;

  return (
    <div style={{ marginTop: 8 }}>
      <Typography.Text strong>API Key của trạm</Typography.Text>
      <div style={{ marginTop: 8, marginBottom: 8 }}>
        {currentKey ? (
          <>
            <Tag color={currentKey.isActive ? 'green' : 'default'}>
              {currentKey.isActive ? 'Active' : 'Revoked'}
            </Tag>
            <Typography.Text type="secondary">
              Cấp lúc: {dayjs(currentKey.createdAtUtc).format(DATE_FORMAT)}
              {currentKey.revokedAtUtc && ` — Thu hồi lúc: ${dayjs(currentKey.revokedAtUtc).format(DATE_FORMAT)}`}
            </Typography.Text>
          </>
        ) : (
          <Typography.Text type="secondary">Trạm chưa được cấp API Key nào.</Typography.Text>
        )}
      </div>

      <div style={{ display: 'flex', gap: 8 }}>
        {(!currentKey || !currentKey.isActive) && (
          <Button size="small" loading={issueMutation.isPending} onClick={handleIssue}>
            Cấp API Key
          </Button>
        )}
        {currentKey?.isActive && (
          <>
            <Button size="small" loading={reissueMutation.isPending} onClick={handleReissue}>
              Cấp lại
            </Button>
            <Button size="small" danger loading={revokeMutation.isPending} onClick={handleRevoke}>
              Thu hồi
            </Button>
          </>
        )}
      </div>

      <StationApiKeyRevealModal issuedKey={revealedKey} onClose={() => setRevealedKey(null)} />
    </div>
  );
}
