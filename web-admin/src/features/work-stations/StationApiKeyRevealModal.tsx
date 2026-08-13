import { Alert, Modal, Typography } from 'antd';
import type { IssuedStationApiKey } from '../../types/stationApiKey';

interface StationApiKeyRevealModalProps {
  issuedKey: IssuedStationApiKey | null;
  onClose: () => void;
}

/**
 * Modal hiển thị giá trị thô API Key đúng 1 lần duy nhất ngay sau khi cấp/cấp lại (US-04a AC1/AC2/AC4) — đóng
 * modal này rồi thì không có cách nào xem lại giá trị thô, chỉ còn metadata (trạng thái/ngày cấp).
 */
export function StationApiKeyRevealModal({ issuedKey, onClose }: StationApiKeyRevealModalProps) {
  return (
    <Modal
      title="API Key đã cấp"
      open={issuedKey !== null}
      onOk={onClose}
      onCancel={onClose}
      okText="Đã sao chép, đóng lại"
      cancelButtonProps={{ style: { display: 'none' } }}
      closable={false}
      maskClosable={false}
    >
      <Alert
        type="warning"
        showIcon
        message="Sao chép ngay bây giờ"
        description="Giá trị này chỉ hiển thị đúng 1 lần. Sau khi đóng cửa sổ này, hệ thống sẽ không hiển thị lại được — hãy dán ngay vào file cấu hình cục bộ (appsettings.json) của trạm."
        style={{ marginBottom: 16 }}
      />
      <Typography.Paragraph
        copyable={{ text: issuedKey?.apiKey ?? '' }}
        code
        style={{ wordBreak: 'break-all', marginBottom: 0 }}
      >
        {issuedKey?.apiKey}
      </Typography.Paragraph>
    </Modal>
  );
}
