import { Drawer, Segmented, Space, Table, Tag, Typography } from 'antd';
import type { ColumnsType, TablePaginationConfig } from 'antd/es/table';
import dayjs from 'dayjs';
import { useEffect, useState } from 'react';
import { useScanHistory } from './useScanHistory';
import type { ReworkStatus, ScanHistoryItem, ScanHistoryQuery, ScanResult } from '../../types/scanHistory';

const DATE_FORMAT = 'DD/MM/YYYY HH:mm:ss';
const DEFAULT_PAGE_SIZE = 20;

/** Cùng bộ màu Kết quả scan đã dùng ở màn hình trạm (US-08/US-18) — chỉ dùng lại ở đây cho mục đích hiển thị. */
const RESULT_LABEL: Record<ScanResult, string> = {
  Ok: 'OK',
  Ng: 'NG',
  DuplicateTag: 'Trùng tem',
  PreviousStageNotPassed: 'Chưa qua công đoạn trước',
  WaitingReworkUnlock: 'Chờ mở khóa rework',
};

const RESULT_COLOR: Record<ScanResult, string> = {
  Ok: 'green',
  Ng: 'red',
  DuplicateTag: 'orange',
  PreviousStageNotPassed: 'orange',
  WaitingReworkUnlock: 'orange',
};

/** US-21 AC10 — nhãn trạng thái rework, đúng theo 4 trạng thái đã chốt trong AC. */
const REWORK_STATUS_LABEL: Record<ReworkStatus, string> = {
  NotUnlocked: 'Chưa mở khóa',
  WaitingRescan: 'Đã mở khóa, chờ scan lại',
  Fixed: 'Đã sửa xong (scan lại OK)',
  StillNg: 'Đã scan lại nhưng vẫn NG',
};

const REWORK_STATUS_COLOR: Record<ReworkStatus, string> = {
  NotUnlocked: 'red',
  WaitingRescan: 'orange',
  Fixed: 'green',
  StillNg: 'red',
};

/** Lựa chọn filter kết quả scan hiển thị trên UI (nâng cấp UX 19/08/2026) — chỉ OK/NG, không gồm 3 kết quả từ chối tự động. */
type ResultFilter = 'All' | 'Ok' | 'Ng';

const RESULT_FILTER_OPTIONS: { label: string; value: ResultFilter }[] = [
  { label: 'Tất cả', value: 'All' },
  { label: 'OK', value: 'Ok' },
  { label: 'NG', value: 'Ng' },
];

export interface ScanHistoryDrilldownTarget {
  lineId: number;
  lineName: string;
  stageId: number;
  stageName: string;
  lot: string;
  from?: string;
  to?: string;
}

interface ScanHistoryDrilldownDrawerProps {
  target: ScanHistoryDrilldownTarget | null;
  onClose: () => void;
}

/**
 * US-21 AC7-AC11 — drill-down danh sách lượt scan chi tiết (OK lẫn NG) của đúng 1 dòng (Line, Công đoạn) trong
 * chi tiết 1 Lot (AC7), kèm Trạm thực hiện (AC8), lý do NG + người xác nhận (AC9), trạng thái rework + người sửa
 * hàng (AC10/AC11). Tái sử dụng nguyên vẹn `GET api/v1/scans/history` (US-10) đã bổ sung filter Lot/StageId/Result
 * — không gọi API riêng. Dùng chung cho cả `LotReportTab` (entrypoint chính, vòng 3) lẫn `LineReportTab` (nhánh
 * phụ, vòng 1/2 — xem AC6).
 *
 * Nâng cấp UX 19/08/2026: đổi từ `Modal` sang `Drawer` trượt phải (giữ ngữ cảnh trang báo cáo đang xem phía sau),
 * thêm phân trang thật (thay `pageSize: 200` cố định + `pagination={false}`), filter theo kết quả OK/NG, và badge
 * tổng số OK/NG. Badge OK/NG dùng 2 query đếm riêng (`pageSize: 1`, đọc `totalCount`) LUÔN theo đúng bộ filter
 * Line/Stage/Lot/from-to của `target` — KHÔNG áp filter OK/NG đang chọn để xem bảng, để không gây hiểu lầm khi
 * người dùng đang lọc "chỉ xem NG" (badge phải cố định theo ngữ cảnh, độc lập filter hiển thị).
 */
export function ScanHistoryDrilldownDrawer({ target, onClose }: ScanHistoryDrilldownDrawerProps) {
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(DEFAULT_PAGE_SIZE);
  const [resultFilter, setResultFilter] = useState<ResultFilter>('All');

  // Reset trang + filter về mặc định mỗi khi chọn 1 dòng (Line/Stage) hoặc Lot/khoảng thời gian khác — tránh giữ
  // lại trang/filter cũ khi người dùng bấm sang dòng khác trong khi Drawer đang mở.
  useEffect(() => {
    setPage(1);
    setResultFilter('All');
  }, [target?.lineId, target?.stageId, target?.lot, target?.from, target?.to]);

  const baseQuery: Omit<ScanHistoryQuery, 'result' | 'page' | 'pageSize'> | null = target
    ? {
        lineId: target.lineId,
        stageId: target.stageId,
        lot: target.lot,
        from: target.from,
        to: target.to,
      }
    : null;

  const query: ScanHistoryQuery | null = baseQuery
    ? {
        ...baseQuery,
        result: resultFilter === 'All' ? undefined : resultFilter,
        page,
        pageSize,
      }
    : null;

  const historyQuery = useScanHistory(query);

  // 2 query đếm OK/NG — KHÔNG áp resultFilter đang chọn (xem remarks ở JSDoc trên), luôn phản ánh đúng tổng của
  // toàn bộ ngữ cảnh (Line, Công đoạn, Lot, khoảng thời gian) đang xem.
  const okCountQuery = useScanHistory(baseQuery ? { ...baseQuery, result: 'Ok', page: 1, pageSize: 1 } : null);
  const ngCountQuery = useScanHistory(baseQuery ? { ...baseQuery, result: 'Ng', page: 1, pageSize: 1 } : null);

  const columns: ColumnsType<ScanHistoryItem> = [
    { title: 'Mã tem', dataIndex: 'tagCode', key: 'tagCode' },
    {
      title: 'Thời điểm',
      dataIndex: 'scannedAtUtc',
      key: 'scannedAtUtc',
      render: (value: string) => dayjs(value).format(DATE_FORMAT),
    },
    {
      // AC8: Trạm làm việc thực hiện — KHÔNG phải tên cá nhân Operator (xem ghi chú US-21 mục 8.2 SRS).
      title: 'Trạm thực hiện',
      dataIndex: 'workStationName',
      key: 'workStationName',
      render: (value: string) => value || '—',
    },
    {
      // AC12 (bổ sung 19/08/2026): snapshot ProductionPlan.OperatorNames tại thời điểm scan — khai báo CHUNG cho
      // cả kế hoạch, KHÔNG phải định danh cá nhân theo từng lượt scan (xem mục 8.2 SRS).
      title: 'Nhân viên',
      dataIndex: 'operatorNames',
      key: 'operatorNames',
      render: (value: string) => value || '—',
    },
    {
      title: 'Kết quả',
      dataIndex: 'result',
      key: 'result',
      render: (result: ScanResult) => <Tag color={RESULT_COLOR[result]}>{RESULT_LABEL[result]}</Tag>,
    },
    {
      // AC9: lý do NG + người xác nhận, chỉ có ý nghĩa với lượt Ng.
      title: 'Lý do NG / Người xác nhận',
      key: 'ngDetail',
      render: (_: unknown, row: ScanHistoryItem) =>
        row.result === 'Ng' ? (
          <span>
            {row.rejectionReason ?? '—'}
            {row.confirmedByUserName ? ` (${row.confirmedByUserName})` : ''}
          </span>
        ) : (
          '—'
        ),
    },
    {
      // AC10: trạng thái rework suy luận động — chỉ có ý nghĩa với lượt Ng.
      title: 'Trạng thái rework',
      key: 'reworkStatus',
      render: (_: unknown, row: ScanHistoryItem) =>
        row.result === 'Ng' && row.reworkStatus ? (
          <Tag color={REWORK_STATUS_COLOR[row.reworkStatus]}>
            {REWORK_STATUS_LABEL[row.reworkStatus]}
            {row.reworkStatus === 'StillNg' && row.reworkStillNgOccurrence ? ` (lần ${row.reworkStillNgOccurrence})` : ''}
          </Tag>
        ) : (
          '—'
        ),
    },
    {
      // AC11: "Người sửa hàng" = người đăng nhập mở khóa rework (US-19 AC7 — bắt buộc re-auth mỗi lần).
      title: 'Người sửa hàng',
      key: 'reworkUnlockedBy',
      render: (_: unknown, row: ScanHistoryItem) =>
        row.result === 'Ng' && row.reworkUnlockedByUserName ? (
          <span>
            {row.reworkUnlockedByUserName}
            {row.reworkUnlockedAtUtc ? ` — ${dayjs(row.reworkUnlockedAtUtc).format(DATE_FORMAT)}` : ''}
            {row.reworkUnlockNote ? ` (${row.reworkUnlockNote})` : ''}
          </span>
        ) : (
          '—'
        ),
    },
  ];

  const pagination: TablePaginationConfig = {
    current: page,
    pageSize,
    total: historyQuery.data?.totalCount ?? 0,
    showSizeChanger: true,
    onChange: (nextPage, nextPageSize) => {
      setPage(nextPage);
      setPageSize(nextPageSize);
    },
  };

  return (
    <Drawer
      open={target !== null}
      onClose={onClose}
      placement="right"
      width="min(1000px, 100%)"
      title={
        target
          ? `Chi tiết lượt scan — Line ${target.lineName} / Lot ${target.lot} / ${target.stageName}`
          : 'Chi tiết lượt scan'
      }
    >
      {historyQuery.isError && (
        <Typography.Text type="danger">Không tải được danh sách lượt scan. Vui lòng thử lại.</Typography.Text>
      )}
      <Space direction="vertical" style={{ width: '100%', marginBottom: 12 }} size="middle">
        <Space size="middle" wrap>
          <Typography.Text>
            OK: <Typography.Text strong style={{ color: '#389e0d' }}>{okCountQuery.data?.totalCount ?? '—'}</Typography.Text>
            {' · '}
            NG: <Typography.Text strong style={{ color: '#cf1322' }}>{ngCountQuery.data?.totalCount ?? '—'}</Typography.Text>
          </Typography.Text>
        </Space>
        <Segmented
          options={RESULT_FILTER_OPTIONS}
          value={resultFilter}
          onChange={(value) => {
            setResultFilter(value as ResultFilter);
            setPage(1);
          }}
        />
      </Space>
      <Table<ScanHistoryItem>
        rowKey="id"
        size="small"
        loading={historyQuery.isLoading}
        columns={columns}
        dataSource={historyQuery.data?.items ?? []}
        pagination={pagination}
        scroll={{ x: 1050 }}
      />
    </Drawer>
  );
}
