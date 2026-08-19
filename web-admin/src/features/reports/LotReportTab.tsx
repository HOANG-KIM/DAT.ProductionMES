import { Alert, AutoComplete, Card, DatePicker, Descriptions, Empty, Space, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import type { Dayjs } from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { useLotSearch } from './useLotSearch';
import { useLotSummary } from './useLotSummary';
import { ScanHistoryDrilldownModal } from './ScanHistoryDrilldownModal';
import type { ScanHistoryDrilldownTarget } from './ScanHistoryDrilldownModal';
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
 * chọn (AC5) → bấm 1 dòng mở drill-down chi tiết lượt scan (AC7-AC11, tái dùng `ScanHistoryDrilldownModal`).
 * KHÔNG có PLAN/BALANCE (AC6 — tạm hoãn, xem `LineReportTab` cho nhánh phụ giữ PLAN/BALANCE).
 */
export function LotReportTab() {
  const [searchText, setSearchText] = useState('');
  const [selectedLot, setSelectedLot] = useState<string | null>(null);
  const [range, setRange] = useState<DateRange | null>(null);
  const [drilldownTarget, setDrilldownTarget] = useState<ScanHistoryDrilldownTarget | null>(null);

  const debouncedSearchText = useDebouncedValue(searchText, 300);
  const searchQuery = useLotSearch(debouncedSearchText);

  const summaryFrom = range ? range[0].toISOString() : undefined;
  const summaryTo = range ? range[1].toISOString() : undefined;
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
          placeholder="Nhập (một phần) mã Lot cần tra cứu..."
        />

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
            <Card size="small" title={`Thông tin tổng quan — Lot ${summaryQuery.data.lot}`}>
              <Descriptions column={1} size="small">
                <Descriptions.Item label="Model">{renderMultiValue(summaryQuery.data.models)}</Descriptions.Item>
                <Descriptions.Item label="Khách hàng">{renderMultiValue(summaryQuery.data.customers)}</Descriptions.Item>
                <Descriptions.Item label="Revision">{renderMultiValue(summaryQuery.data.revisions)}</Descriptions.Item>
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

      <ScanHistoryDrilldownModal target={drilldownTarget} onClose={() => setDrilldownTarget(null)} />
    </div>
  );
}
