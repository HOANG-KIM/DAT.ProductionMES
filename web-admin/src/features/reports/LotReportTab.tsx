import { Alert, AutoComplete, Button, Card, DatePicker, Descriptions, Empty, Input, message, Space, Spin, Table, Tag, Typography } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { exportLotReport } from '../../api/lotReportsApi';
import { useLotSearch } from './useLotSearch';
import { useLotSummary } from './useLotSummary';
import { ScanHistoryDrilldownDrawer } from './ScanHistoryDrilldownDrawer';
import type { ScanHistoryDrilldownTarget } from './ScanHistoryDrilldownDrawer';
import type { LotStageRow } from '../../types/lotReport';

const { RangePicker } = DatePicker;

/** Định dạng ngày giờ hiển thị theo giờ Việt Nam (API-Conventions.md mục 10 — chỉ quy đổi ở tầng hiển thị). */
const DATE_FORMAT = 'DD/MM/YYYY HH:mm';

type DateRange = [Dayjs, Dayjs];

/** Debounce nhẹ cho ô tìm kiếm Lot (AC1) — tránh gọi API dồn dập theo từng ký tự gõ. */
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

/**
 * US-21 (vòng 3, 18/08/2026) — entrypoint chính của báo cáo, Lot làm trục chính: tìm/chọn 1 Lot (AC1/AC2) → chi
 * tiết Model/Khách hàng/Revision (AC3) → danh sách (Line, Công đoạn) kèm OK/NG (AC4) → lọc khoảng thời gian tùy
 * chọn (AC5) → bấm 1 dòng mở drill-down chi tiết lượt scan (AC7-AC11, tái dùng `ScanHistoryDrilldownDrawer`).
 * KHÔNG có PLAN/BALANCE (AC6 — tạm hoãn, xem `LineReportTab` cho nhánh phụ giữ PLAN/BALANCE).
 */
export function LotReportTab() {
  const [searchText, setSearchText] = useState('');
  const [selectedLot, setSelectedLot] = useState<string | null>(null);
  const [range, setRange] = useState<DateRange | null>(null);
  const [drilldownTarget, setDrilldownTarget] = useState<ScanHistoryDrilldownTarget | null>(null);
  const [isExporting, setIsExporting] = useState(false);

  const debouncedSearchText = useDebouncedValue(searchText, 300);
  const searchQuery = useLotSearch(debouncedSearchText);

  // Đổi ý 19/08/2026: gửi giờ local KHÔNG quy đổi UTC (không dùng toISOString — luôn quy về UTC) — khớp với
  // Scan.ScannedAtUtc backend nay cũng lưu giờ local (xem API-Conventions.md mục 10).
  const summaryFrom = range ? range[0].format('YYYY-MM-DDTHH:mm:ss') : undefined;
  const summaryTo = range ? range[1].format('YYYY-MM-DDTHH:mm:ss') : undefined;
  const summaryQuery = useLotSummary(selectedLot, { from: summaryFrom, to: summaryTo });

  const options = useMemo(
    () => (searchQuery.data ?? []).map((item) => ({ value: item.lot, label: item.lot })),
    [searchQuery.data],
  );

  const openDrilldown = (row: LotStageRow) => {
    if (!selectedLot) {
      return;
    }
    setDrilldownTarget({
      lineId: row.lineId,
      lineName: row.lineName,
      stageId: row.stageId,
      stageName: row.stageName,
      lot: selectedLot,
      from: summaryFrom,
      to: summaryTo,
    });
  };

  // US-23 (FR-23, phạm vi thu hẹp — chỉ tab "Theo Lot"): tải file .xlsx cùng bộ lọc (Lot + khoảng thời gian) đang xem.
  const handleExport = async () => {
    if (!selectedLot) {
      return;
    }
    setIsExporting(true);
    try {
      const blob = await exportLotReport(selectedLot, { from: summaryFrom, to: summaryTo });
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `bao-cao-lot-${selectedLot}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
    } catch {
      void message.error('Xuất Excel thất bại, vui lòng thử lại');
    } finally {
      setIsExporting(false);
    }
  };

  const renderMultiValue = (values: string[], emptyLabel = '(trống)') => {
    if (values.length === 0) {
      return '—';
    }
    if (values.length === 1) {
      return values[0] || emptyLabel;
    }
    return (
      <Space direction="vertical" size="small">
        <Tag color="warning">Không đồng nhất giữa các kế hoạch cùng Lot</Tag>
        <Space wrap>
          {values.map((value) => (
            <Tag key={value || emptyLabel}>{value || emptyLabel}</Tag>
          ))}
        </Space>
      </Space>
    );
  };

  const columns: ColumnsType<LotStageRow> = [
    { title: 'Line', dataIndex: 'lineName', key: 'lineName' },
    { title: 'Công đoạn', dataIndex: 'stageName', key: 'stageName' },
    {
      title: 'OK',
      dataIndex: 'okCount',
      key: 'okCount',
      align: 'right',
      render: (value: number) => <span style={{ color: '#4CAF50', fontWeight: 600 }}>{value}</span>,
    },
    {
      title: 'NG',
      dataIndex: 'ngCount',
      key: 'ngCount',
      align: 'right',
      render: (value: number) => <span style={{ color: value > 0 ? '#E53935' : undefined, fontWeight: 600 }}>{value}</span>,
    },
    {
      // US-21a AC5: so sánh Đủ/Chưa đủ theo TỪNG dòng riêng biệt (KHÔNG gộp 1 số cho cả Lot).
      title: 'Đủ số lượng Lot?',
      dataIndex: 'isSufficientQuantity',
      key: 'isSufficientQuantity',
      align: 'center',
      render: (value: boolean | null) => {
        if (value === null) {
          return <Tag>Chưa xác định</Tag>;
        }
        return value ? <Tag color="success">Đủ</Tag> : <Tag color="error">Chưa đủ</Tag>;
      },
    },
  ];

  return (
    <div>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <AutoComplete
          style={{ width: 320 }}
          options={options}
          value={searchText}
          onSearch={setSearchText}
          onSelect={(value: string) => {
            setSelectedLot(value);
            setSearchText(value);
          }}
          onClear={() => {
            setSelectedLot(null);
            setSearchText('');
          }}
          allowClear
        >
          {/*
            AutoComplete (antd v6) không forward prop `autoComplete` xuống input bên trong (rc-select v6 chỉ mặc định
            gán `autoComplete="new-password"`, không đọc từ props ngoài — xem @rc-component/select/es/SelectInput/
            Content/index.js). Truyền input tuỳ biến qua children (cơ chế customize input chính thức của antd) là
            cách DUY NHẤT thật sự đặt được `autoComplete="off"` lên thẻ <input> DOM để tắt gợi ý lịch sử nhập liệu
            của trình duyệt cho ô tìm Lot.
          */}
          <Input autoComplete="off" placeholder="Nhập (một phần) mã Lot cần tra cứu..." />
        </AutoComplete>

        {selectedLot === null && (
          <Alert
            type="info"
            showIcon
            message="Nhập mã Lot ở ô tìm kiếm phía trên rồi chọn 1 kết quả gợi ý để xem chi tiết vòng đời sản xuất."
          />
        )}

        {summaryQuery.isNotFound && (
          <Alert
            type="warning"
            showIcon
            message="Không tìm thấy Lot"
            description={`Không có kế hoạch sản xuất nào đã từng dùng Lot "${selectedLot}".`}
          />
        )}

        {summaryQuery.isError && !summaryQuery.isNotFound && (
          <Alert type="error" showIcon message="Không tải được chi tiết Lot" description="Vui lòng thử lại." />
        )}

        {summaryQuery.isLoading && selectedLot !== null && (
          <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />
        )}

        {summaryQuery.data && (
          <>
            <Card
              size="small"
              title={`Thông tin tổng quan — Lot ${summaryQuery.data.lot}`}
              extra={
                <Button icon={<DownloadOutlined />} onClick={() => void handleExport()} loading={isExporting}>
                  Xuất Excel
                </Button>
              }
            >
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Model">{renderMultiValue(summaryQuery.data.models)}</Descriptions.Item>
                <Descriptions.Item label="Khách hàng">{renderMultiValue(summaryQuery.data.customers)}</Descriptions.Item>
                <Descriptions.Item label="Revision">{renderMultiValue(summaryQuery.data.revisions)}</Descriptions.Item>
                {/* FR-21b (bổ sung 19/08/2026): MIN(ScannedAtUtc) trên TOÀN BỘ lượt scan của Lot, KHÔNG phụ thuộc RangePicker bên dưới. */}
                <Descriptions.Item label="Thời gian bắt đầu">
                  {summaryQuery.data.firstScannedAtUtc === null
                    ? 'Chưa xác định'
                    : dayjs(summaryQuery.data.firstScannedAtUtc).format(DATE_FORMAT)}
                </Descriptions.Item>
                {/* US-21a AC1/AC5/AC6 (viết lại hoàn toàn 19/08/2026): "Số lượng Lot" nhập tay tại Station.Wpf (US-05 AC7-AC9), KHÔNG phải SUM. */}
                <Descriptions.Item label="Số lượng Lot">
                  <Typography.Text strong>
                    {summaryQuery.data.lotTotalQuantity === null
                      ? 'Chưa xác định'
                      : summaryQuery.data.lotTotalQuantity.toLocaleString('vi-VN')}
                  </Typography.Text>
                </Descriptions.Item>
              </Descriptions>
            </Card>

            <Space wrap>
              <RangePicker
                showTime
                format={DATE_FORMAT}
                value={range}
                onChange={(values) => {
                  if (!values || !values[0] || !values[1]) {
                    setRange(null);
                    return;
                  }
                  setRange([values[0], values[1]]);
                }}
              />
              <Typography.Text type="secondary">
                {range
                  ? `Đang lọc OK/NG từ ${range[0].format(DATE_FORMAT)} đến ${range[1].format(DATE_FORMAT)}.`
                  : 'Chưa chọn khoảng thời gian — OK/NG tính trên toàn bộ lịch sử của Lot.'}
              </Typography.Text>
            </Space>

            <Table<LotStageRow>
              rowKey={(row) => `${row.lineId}-${row.stageId}`}
              columns={columns}
              dataSource={summaryQuery.data.rows}
              pagination={false}
              locale={{ emptyText: <Empty description="Lot này chưa có (Line, Công đoạn) nào" /> }}
              onRow={(row) => ({
                onClick: () => openDrilldown(row),
                style: { cursor: 'pointer' },
              })}
            />
          </>
        )}
      </Space>

      <ScanHistoryDrilldownDrawer target={drilldownTarget} onClose={() => setDrilldownTarget(null)} />
    </div>
  );
}
