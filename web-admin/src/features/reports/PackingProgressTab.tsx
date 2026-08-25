import { Alert, AutoComplete, Button, Empty, Input, message, Progress, Select, Space, Spin, Table, Tag, Typography } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import dayjs from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { usePackingProgressReport } from './usePackingProgressReport';
import { usePackingProgressSearch } from './usePackingProgressSearch';
import { PackingBoxesModal } from './PackingBoxesModal';
import { exportPackingProgressReport } from '../../api/packingProgressReportsApi';
import type { PackingProgressPlanStatus, PackingProgressReportRow } from '../../types/packingProgressReport';

/** Định dạng ngày giờ hiển thị theo giờ Việt Nam (API-Conventions.md mục 10 — chỉ quy đổi ở tầng hiển thị). */
const DATE_FORMAT = 'DD/MM/YYYY HH:mm';

/**
 * Nhãn tiếng Việt + màu Tag cho cột "Trạng thái kế hoạch" (AC14, viết lại LẦN 3 — 25/08/2026) — chưa có quy ước
 * nhãn PlanStatus nào khác trong hệ thống tính tới nay, đặt mới riêng cho màn hình này.
 */
const PLAN_STATUS_LABEL: Record<PackingProgressPlanStatus, { label: string; color: string }> = {
  Running: { label: 'Đang chạy', color: 'success' },
  Paused: { label: 'Tạm dừng', color: 'warning' },
  Completed: { label: 'Hoàn thành', color: 'blue' },
  Cancelled: { label: 'Đã hủy', color: 'error' },
};

/** Debounce nhẹ cho ô tìm kiếm Lot (AC1) — tránh gọi API dồn dập theo từng ký tự gõ, cùng pattern `LotReportTab`. */
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);

  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);

  return debounced;
}

/**
 * US-26 (FR-26) — theo dõi tiến độ đóng thùng ở mức quản lý. Viết lại toàn bộ 25/08/2026 (AC1-AC5): thay mô hình
 * "tự tải toàn bộ danh sách + polling 15s" bằng tra cứu 1 Lot cụ thể — gõ (một phần) mã Lot vào ô tìm kiếm (AC1,
 * tái dùng NGUYÊN VẸN pattern `AutoComplete`/debounce 300ms của `LotReportTab`, US-21), chọn 1 gợi ý rồi xem (các)
 * dòng kết quả tương ứng (AC2/AC3/AC4). Viết lại LẦN 2 cùng ngày (sau phản hồi người giao việc): gợi ý autocomplete
 * gộp DUY NHẤT theo Lot (AC1, không còn lặp theo Line), bổ sung dropdown "Lọc theo Line" + dòng "Tổng cộng (các
 * Line đang hiển thị)" khi Lot đó chạy ≥2 Line (AC2.2/AC2.3). KHÔNG còn polling/tự động cập nhật nào (AC5) — muốn
 * xem số liệu mới phải tra cứu lại. Đặt làm tab thứ 3 trong `ProductionReportPage` (cạnh "Theo Lot"/"Theo Line"),
 * cùng permission `Report.View`.
 */
export function PackingProgressTab() {
  const [searchText, setSearchText] = useState('');
  const [selectedLot, setSelectedLot] = useState<string | null>(null);
  // AC2.2: Line đang lọc trong bảng kết quả — null = "Tất cả Line" (lọc phía client, không gọi lại API).
  const [lineFilter, setLineFilter] = useState<number | null>(null);
  // AC6: dòng báo cáo đang xem chi tiết thùng — null = đóng modal (xem PackingBoxesModal).
  const [boxesFilter, setBoxesFilter] = useState<{ lineId: number; lot: string; lineName: string } | null>(null);
  // AC9: key (lineId-lot) của dòng đang xuất Excel — null = không có dòng nào đang xuất (xuất theo TỪNG DÒNG, không phải nút chung).
  const [exportingKey, setExportingKey] = useState<string | null>(null);

  const debouncedSearchText = useDebouncedValue(searchText, 300);
  const searchQuery = usePackingProgressSearch(debouncedSearchText);

  // AC2: chỉ truyền Lot đã chọn — hiển thị ĐẦY ĐỦ mọi Line đang chạy Lot đó (AC4), không lọc theo LineId/Model.
  const reportQuery = usePackingProgressReport({ lot: selectedLot ?? undefined });

  // AC1 (viết lại LẦN 2): gợi ý CHỈ hiển thị mã Lot — giống hệt `useLotSearch`/`LotReportTab` (US-21).
  const options = useMemo(
    () => (searchQuery.data ?? []).map((item) => ({ value: item.lot, label: item.lot })),
    [searchQuery.data],
  );

  const rows = useMemo(() => reportQuery.data?.rows ?? [], [reportQuery.data]);

  // AC2.2: danh sách Line xuất hiện trong kết quả, dedupe theo lineId — chỉ hiển thị dropdown khi >=2 dòng.
  const lineOptions = useMemo(() => {
    const map = new Map<number, string>();
    rows.forEach((row) => map.set(row.lineId, row.lineName));
    return Array.from(map.entries()).map(([id, name]) => ({ value: id, label: name }));
  }, [rows]);

  const showLineFilter = rows.length >= 2;
  const displayedRows = showLineFilter && lineFilter !== null ? rows.filter((row) => row.lineId === lineFilter) : rows;
  // AC2.3: dòng "Tổng cộng" chỉ hiện khi đang ở "Tất cả Line" VÀ có >=2 dòng kết quả.
  const showTotalRow = showLineFilter && lineFilter === null;

  // AC2.3: SUM số thùng đã đóng + tổng SL đã đóng OK của các dòng đang hiển thị, đối chiếu lại với
  // `Lot.TotalQuantity` (giống nhau ở mọi dòng cùng Lot — dùng lại giá trị có sẵn trên dòng, không gọi API riêng).
  const totalSummary = useMemo(() => {
    if (!showTotalRow) {
      return null;
    }
    const totalBoxCount = displayedRows.reduce((sum, row) => sum + row.completedBoxCount, 0);
    const totalPackedOkQuantity = displayedRows.reduce((sum, row) => sum + row.packedOkQuantity, 0);
    const lotTotalQuantity = displayedRows[0]?.lotTotalQuantity ?? null;

    if (lotTotalQuantity === null) {
      return { totalBoxCount, totalPackedOkQuantity, lotTotalQuantity: null, completionPercentage: null, isSufficientQuantity: null };
    }

    // Cùng công thức làm tròn 2 chữ số + guard chia 0 đã dùng ở AC2 gốc (PackingProgressReportService.GetReportAsync).
    const completionPercentage =
      lotTotalQuantity > 0
        ? Math.round((totalPackedOkQuantity / lotTotalQuantity) * 100 * 100) / 100
        : totalPackedOkQuantity > 0
          ? 100
          : 0;
    const isSufficientQuantity = totalPackedOkQuantity >= lotTotalQuantity;

    return { totalBoxCount, totalPackedOkQuantity, lotTotalQuantity, completionPercentage, isSufficientQuantity };
  }, [showTotalRow, displayedRows]);

  // AC9: xuất Excel theo TỪNG DÒNG (Line + Lot), KHÔNG phải nút xuất chung cho toàn bảng.
  const handleExport = async (row: PackingProgressReportRow) => {
    const key = `${row.lineId}-${row.lot}`;
    setExportingKey(key);
    try {
      const blob = await exportPackingProgressReport(row.lineId, row.lot);
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `bao-cao-lich-su-dong-thung-${row.lot}-${dayjs().format('YYYYMMDD')}.xlsx`;
      document.body.appendChild(link);
      link.click();
      link.remove();
      window.URL.revokeObjectURL(url);
      void message.success('Đã xuất báo cáo Excel');
    } catch {
      void message.error('Xuất Excel thất bại, vui lòng thử lại');
    } finally {
      setExportingKey(null);
    }
  };

  const columns: ColumnsType<PackingProgressReportRow> = [
    { title: 'Line', dataIndex: 'lineName', key: 'lineName' },
    { title: 'Model', dataIndex: 'model', key: 'model' },
    { title: 'Lot', dataIndex: 'lot', key: 'lot' },
    {
      // AC14: cột "Trạng thái kế hoạch" — trạng thái của bản ghi đại diện khi 1 (Line, Đóng thùng) có nhiều lịch sử PlanStatus.
      title: 'Trạng thái kế hoạch',
      dataIndex: 'planStatus',
      key: 'planStatus',
      align: 'center',
      render: (value: PackingProgressPlanStatus) => {
        const { label, color } = PLAN_STATUS_LABEL[value];
        return <Tag color={color}>{label}</Tag>;
      },
    },
    {
      title: 'Số thùng đã đóng',
      dataIndex: 'completedBoxCount',
      key: 'completedBoxCount',
      align: 'right',
    },
    {
      title: 'Tổng SL đã đóng (OK)',
      dataIndex: 'packedOkQuantity',
      key: 'packedOkQuantity',
      align: 'right',
      render: (value: number) => <span style={{ fontWeight: 600 }}>{value.toLocaleString('vi-VN')}</span>,
    },
    {
      title: 'Tổng số lượng Lot',
      dataIndex: 'lotTotalQuantity',
      key: 'lotTotalQuantity',
      align: 'right',
      render: (value: number | null) => (value === null ? <Tag>Chưa xác định</Tag> : value.toLocaleString('vi-VN')),
    },
    {
      // AC2/AC3: % hoàn thành — "Chưa xác định" khi Lot chưa nhập Tổng số lượng, KHÔNG suy diễn 0%.
      title: '% hoàn thành',
      dataIndex: 'completionPercentage',
      key: 'completionPercentage',
      width: 220,
      render: (value: number | null, row) => {
        if (value === null) {
          return <Tag>Chưa xác định</Tag>;
        }
        return (
          <Progress
            percent={Math.min(value, 100)}
            format={() => `${value}%`}
            status={row.isSufficientQuantity ? 'success' : 'active'}
          />
        );
      },
    },
    {
      // Dùng lại đúng nhãn Đủ/Chưa đủ đã chốt ở US-21/US-21a — không có khái niệm "hoàn thành" riêng khác (AC2).
      title: 'Trạng thái',
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
    {
      // AC6/AC9: drill-down xem danh sách thùng chi tiết + xuất Excel, cả 2 đều theo TỪNG DÒNG (Line + Lot).
      title: '',
      key: 'action',
      width: 240,
      render: (_, row) => (
        <Space>
          <Button size="small" onClick={() => setBoxesFilter({ lineId: row.lineId, lot: row.lot, lineName: row.lineName })}>
            Xem thùng
          </Button>
          <Button
            size="small"
            icon={<DownloadOutlined />}
            loading={exportingKey === `${row.lineId}-${row.lot}`}
            onClick={() => void handleExport(row)}
          >
            Xuất Excel
          </Button>
        </Space>
      ),
    },
  ];

  return (
    <div>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <AutoComplete
          style={{ width: 420 }}
          options={options}
          value={searchText}
          onSearch={setSearchText}
          onSelect={(value: string) => {
            setSelectedLot(value);
            setSearchText(value);
            setLineFilter(null);
          }}
          onClear={() => {
            setSelectedLot(null);
            setSearchText('');
            setLineFilter(null);
          }}
          allowClear
        >
          {/* AutoComplete (antd v6) không forward autoComplete xuống input bên trong — truyền input tuỳ biến qua children (cùng cách LotReportTab). */}
          <Input autoComplete="off" placeholder="Nhập (một phần) mã Lot đang đóng thùng cần tra cứu..." />
        </AutoComplete>

        {selectedLot === null && (
          <Alert
            type="info"
            showIcon
            message="Nhập mã Lot ở ô tìm kiếm phía trên rồi chọn 1 kết quả gợi ý để xem tiến độ đóng thùng. Chỉ gợi ý các Lot đang chạy công đoạn Đóng thùng (trạng thái Running)."
          />
        )}

        {reportQuery.isError && (
          <Alert
            type="error"
            showIcon
            message="Không tải được báo cáo"
            description="Vui lòng thử tra cứu lại."
          />
        )}

        {reportQuery.isLoading && selectedLot !== null && (
          <Spin size="large" style={{ display: 'block', margin: '48px auto' }} />
        )}

        {selectedLot !== null && reportQuery.data && (
          <>
            {/* AC2.2: chỉ hiển thị dropdown lọc Line khi Lot đó có >=2 dòng kết quả — lọc phía client, không gọi lại API. */}
            {showLineFilter && (
              <Space>
                <Typography.Text>Lọc theo Line:</Typography.Text>
                <Select
                  style={{ width: 220 }}
                  value={lineFilter ?? undefined}
                  placeholder="Tất cả Line"
                  allowClear
                  onClear={() => setLineFilter(null)}
                  onChange={(value: number | undefined) => setLineFilter(value ?? null)}
                  options={lineOptions}
                />
              </Space>
            )}

            <Table<PackingProgressReportRow>
              rowKey={(row) => `${row.productionPlanId}-${row.stageId}`}
              columns={columns}
              dataSource={displayedRows}
              pagination={false}
              locale={{
                emptyText: (
                  <Empty description={`Lot "${selectedLot}" hiện không có kế hoạch nào đang Running tại công đoạn Đóng thùng`} />
                ),
              }}
              summary={() =>
                totalSummary && (
                  // AC2.3: dòng "Tổng cộng (các Line đang hiển thị)" — chỉ hiện ở chế độ "Tất cả Line" với >=2 dòng,
                  // KHÔNG có nút "Xem thùng"/"Xuất Excel" (AC9 — không tương ứng đúng 1 lineId cụ thể).
                  <Table.Summary.Row style={{ fontWeight: 600, background: '#fafafa' }}>
                    {/* colSpan=4: gộp Line/Model/Lot/Trạng thái kế hoạch — dòng Tổng cộng gộp nhiều PlanStatus khác nhau, không có 1 giá trị PlanStatus duy nhất để hiển thị (AC14). */}
                    <Table.Summary.Cell index={0} colSpan={4}>
                      Tổng cộng (các Line đang hiển thị)
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={4} align="right">
                      {totalSummary.totalBoxCount.toLocaleString('vi-VN')}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={5} align="right">
                      {totalSummary.totalPackedOkQuantity.toLocaleString('vi-VN')}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={6} align="right">
                      {totalSummary.lotTotalQuantity === null ? (
                        <Tag>Chưa xác định</Tag>
                      ) : (
                        totalSummary.lotTotalQuantity.toLocaleString('vi-VN')
                      )}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={7}>
                      {totalSummary.completionPercentage === null ? (
                        <Tag>Chưa xác định</Tag>
                      ) : (
                        <Progress
                          percent={Math.min(totalSummary.completionPercentage, 100)}
                          format={() => `${totalSummary.completionPercentage}%`}
                          status={totalSummary.isSufficientQuantity ? 'success' : 'active'}
                        />
                      )}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={8} align="center">
                      {totalSummary.isSufficientQuantity === null ? (
                        <Tag>Chưa xác định</Tag>
                      ) : totalSummary.isSufficientQuantity ? (
                        <Tag color="success">Đủ</Tag>
                      ) : (
                        <Tag color="error">Chưa đủ</Tag>
                      )}
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={9} />
                  </Table.Summary.Row>
                )
              }
            />
            <Typography.Text type="secondary" style={{ display: 'block', marginTop: 8 }}>
              Tra cứu lúc: {dayjs(reportQuery.data.generatedAtUtc).format(DATE_FORMAT)}
            </Typography.Text>
          </>
        )}
      </Space>

      <PackingBoxesModal filter={boxesFilter} onClose={() => setBoxesFilter(null)} />
    </div>
  );
}
