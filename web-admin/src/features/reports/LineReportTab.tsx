import { Alert, Button, DatePicker, Input, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { useMemo, useState } from 'react';
import { useProductionReport } from './useProductionReport';
import { useLines } from '../lines/useLines';
import { useStages } from '../stages/useStages';
import { ScanHistoryDrilldownDrawer } from './ScanHistoryDrilldownDrawer';
import type { ScanHistoryDrilldownTarget } from './ScanHistoryDrilldownDrawer';
import type { ProductionReportRow } from '../../types/productionReport';

const { RangePicker } = DatePicker;

/** Định dạng ngày giờ hiển thị theo giờ Việt Nam (API-Conventions.md mục 10 — chỉ quy đổi ở tầng hiển thị). */
const DATE_FORMAT = 'DD/MM/YYYY HH:mm';

type DateRange = [Dayjs, Dayjs];

/**
 * US-21 AC6 (vòng 3, 18/08/2026) — bản báo cáo Line-centric vòng 1/2 (group (Line, Công đoạn, Lot) kèm PLAN/
 * BALANCE) GIỮ LẠI làm nhánh phụ (tab), KHÔNG còn là entrypoint chính (xem `ProductionReportPage`, nay là Lot-
 * centric). Nội dung/logic giữ NGUYÊN VẸN so với bản trước khi tách — chỉ đổi từ page thành component con.
 */
export function LineReportTab() {
  const linesQuery = useLines();
  const stagesQuery = useStages();
  const [lineId, setLineId] = useState<number | undefined>(undefined);
  const [stageId, setStageId] = useState<number | undefined>(undefined);
  const [model, setModel] = useState<string>('');
  const [customer, setCustomer] = useState<string>('');
  const [revision, setRevision] = useState<string>('');
  const [range, setRange] = useState<DateRange | null>(null);
  const [drilldownTarget, setDrilldownTarget] = useState<ScanHistoryDrilldownTarget | null>(null);

  const query = useMemo(
    () => ({
      lineId,
      stageId,
      model: model.trim() || undefined,
      customer: customer.trim() || undefined,
      revision: revision.trim() || undefined,
      // Đổi ý 19/08/2026: gửi giờ local KHÔNG quy đổi UTC (không dùng toISOString — luôn quy về UTC) — khớp với
      // Scan.ScannedAtUtc backend nay cũng lưu giờ local (xem API-Conventions.md mục 10).
      from: range ? range[0].format('YYYY-MM-DDTHH:mm:ss') : undefined,
      to: range ? range[1].format('YYYY-MM-DDTHH:mm:ss') : undefined,
    }),
    [lineId, stageId, model, customer, revision, range],
  );

  const reportQuery = useProductionReport(query);
  const isRealtime = range === null;

  const lineOptions = useMemo(
    () => (linesQuery.data ?? []).map((line) => ({ value: line.id, label: line.name })),
    [linesQuery.data],
  );

  const stageOptions = useMemo(
    () => (stagesQuery.data ?? []).map((stage) => ({ value: stage.id, label: stage.name })),
    [stagesQuery.data],
  );

  const openDrilldown = (row: ProductionReportRow) => {
    if (!row.hasPlanData) {
      return;
    }
    setDrilldownTarget({
      lineId: row.lineId,
      lineName: row.lineName,
      stageId: row.stageId,
      stageName: row.stageName,
      lot: row.lot,
      from: query.from,
      to: query.to,
    });
  };

  const columns: ColumnsType<ProductionReportRow> = [
    { title: 'Line', dataIndex: 'lineName', key: 'lineName' },
    { title: 'Lot', dataIndex: 'lot', key: 'lot', render: (lot: string) => lot || '—' },
    { title: 'Công đoạn', dataIndex: 'stageName', key: 'stageName' },
    {
      title: 'ACTUAL',
      dataIndex: 'actual',
      key: 'actual',
      align: 'right',
      render: (actual: number, row) => (row.hasPlanData ? actual : '—'),
    },
    {
      // AC5 (mới): số lượng NG — chỉ đếm ScanResult.Ng.
      title: 'NG',
      dataIndex: 'ngCount',
      key: 'ngCount',
      align: 'right',
      render: (ngCount: number, row) =>
        row.hasPlanData ? <span style={{ color: ngCount > 0 ? '#E53935' : undefined }}>{ngCount}</span> : '—',
    },
    {
      title: 'PLAN',
      dataIndex: 'plan',
      key: 'plan',
      align: 'right',
      render: (plan: number, row) => (row.hasPlanData ? plan : '—'),
    },
    {
      title: 'BALANCE',
      dataIndex: 'balance',
      key: 'balance',
      align: 'right',
      render: (balance: number, row) => {
        if (!row.hasPlanData) {
          return '—';
        }
        // Cùng quy ước màu Balance dương/âm đã chốt ở Andon board Station.Wpf (US-09 AC2/AC3, ADR-007).
        const color = balance >= 0 ? '#4CAF50' : '#E53935';
        return (
          <span style={{ color, fontWeight: 600 }}>
            {balance >= 0 ? `+${balance}` : balance}
          </span>
        );
      },
    },
    {
      title: 'Trạng thái',
      key: 'status',
      render: (_: unknown, row: ProductionReportRow) =>
        row.hasPlanData ? <Tag color="blue">Đang theo dõi</Tag> : <Tag>Chưa có kế hoạch</Tag>,
    },
  ];

  return (
    <div>
      <Space style={{ marginBottom: 16 }} wrap>
        <Select
          allowClear
          placeholder="Tất cả Line"
          style={{ width: 200 }}
          loading={linesQuery.isLoading}
          options={lineOptions}
          value={lineId}
          onChange={(value: number | undefined) => setLineId(value)}
        />
        <Select
          allowClear
          placeholder="Tất cả công đoạn"
          style={{ width: 200 }}
          loading={stagesQuery.isLoading}
          options={stageOptions}
          value={stageId}
          onChange={(value: number | undefined) => setStageId(value)}
        />
        <Input
          placeholder="Model"
          style={{ width: 160 }}
          value={model}
          onChange={(e) => setModel(e.target.value)}
          allowClear
        />
        <Input
          placeholder="Khách hàng"
          style={{ width: 160 }}
          value={customer}
          onChange={(e) => setCustomer(e.target.value)}
          allowClear
        />
        <Input
          placeholder="Revision"
          style={{ width: 140 }}
          value={revision}
          onChange={(e) => setRevision(e.target.value)}
          allowClear
        />
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
        {!isRealtime && <Button onClick={() => setRange(null)}>Xem thời gian thực</Button>}
      </Space>

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 16 }}
        message={
          range === null
            ? 'Đang xem theo thời gian thực — số liệu tự động làm mới mỗi 15 giây. Bấm vào 1 dòng có kế hoạch để xem chi tiết lượt scan.'
            : `Đang xem dữ liệu từ ${range[0].format(DATE_FORMAT)} đến ${range[1].format(DATE_FORMAT)}. Bấm vào 1 dòng có kế hoạch để xem chi tiết lượt scan.`
        }
      />

      {reportQuery.isError && (
        <Alert
          type="error"
          showIcon
          message="Không tải được báo cáo"
          description="Vui lòng thử tải lại trang."
          style={{ marginBottom: 16 }}
        />
      )}

      {reportQuery.isLoading ? (
        <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />
      ) : (
        <Table<ProductionReportRow>
          rowKey={(row) => `${row.lineId}-${row.stageId}-${row.lot}`}
          columns={columns}
          dataSource={reportQuery.data?.rows ?? []}
          pagination={false}
          onRow={(row) => ({
            onClick: () => openDrilldown(row),
            style: { cursor: row.hasPlanData ? 'pointer' : 'default' },
          })}
        />
      )}

      {reportQuery.data && (
        <Typography.Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
          Cập nhật lúc: {dayjs(reportQuery.data.generatedAtUtc).format(DATE_FORMAT)}
        </Typography.Text>
      )}

      <ScanHistoryDrilldownDrawer target={drilldownTarget} onClose={() => setDrilldownTarget(null)} />
    </div>
  );
}
