using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="ScanService.GetHistoryAsync"/> (US-10 AC2/AC3/AC4). Tách riêng khỏi
/// <see cref="ScanServiceTests"/> (vốn tập trung <see cref="ScanService.CreateAsync"/>/AC1) để dễ theo dõi, cùng
/// pattern mock <see cref="IRepository{TEntity}"/> qua <see cref="Mock{T}"/> (không đụng DB thật).
/// </summary>
public class ScanServiceHistoryTests
{
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ReworkUnlock>> _reworkUnlockRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ScanService _sut;

    public ScanServiceHistoryTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        // US-21 AC8/AC10/AC11: GetHistoryAsync tra thêm WorkStation (tên trạm) và ReworkUnlock (trạng thái rework
        // của lượt Ng) — mặc định rỗng cho mọi test ở đây (không tập trung vào AC8/AC10/AC11, xem ScanServiceTests
        // cho các test riêng của 2 AC này).
        _workStationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<WorkStation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkStation>());
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReworkUnlock>());

        _sut = new ScanService(
            _unitOfWorkMock.Object,
            Mock.Of<IProductionPlanStageService>(),
            Mock.Of<IScanNotifier>());
    }

    /// <summary>Mô phỏng repository thật — FindAsync áp đúng predicate của ScanService lên danh sách "DB" cục bộ này (đúng cách ScanServiceTests đang làm).</summary>
    private void SetupExistingScans(List<Scan> existingScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                existingScans.Where(predicate.Compile()).ToList());
    }

    private static Scan MakeScan(int id, string tagCode, int workStationId, int lineId, DateTime scannedAtUtc, ScanResult result = ScanResult.Ok)
        => new()
        {
            Id = id,
            TagCode = tagCode,
            WorkStationId = workStationId,
            LineId = lineId,
            StageId = 100,
            ProductionPlanId = 1,
            ScannedAtUtc = scannedAtUtc,
            Result = result,
            Customer = "Khách hàng A",
            Model = "Model X",
            Lot = "LOT001",
            Revision = "B",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30m,
            OperatorNames = "Nguyễn Văn A, Trần Thị B",
        };

    // AC2 — Tra cứu theo tem: trả về toàn bộ lượt scan liên quan tới tem đó, sắp theo thời gian tăng dần.
    [Fact]
    public async Task GetHistoryAsync_TimTheoTem_TraVeToanBoLuotScanCuaTemSapXepTheoThoiGian()
    {
        var t0 = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "TAG-A", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(10)),
            MakeScan(2, "TAG-A", workStationId: 2, lineId: 1, scannedAtUtc: t0),
            MakeScan(3, "TAG-B", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(5)),
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { TagCode = "TAG-A" });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        // Sắp xếp theo ScannedAtUtc tăng dần: bản ghi Id=2 (sớm hơn) phải đứng trước Id=1.
        Assert.Equal(2, result.Items[0].Id);
        Assert.Equal(1, result.Items[1].Id);
        Assert.All(result.Items, item => Assert.Equal("TAG-A", item.TagCode));
    }

    // AC3 — Lọc riêng theo trạm.
    [Fact]
    public async Task GetHistoryAsync_LocTheoTram_ChiTraVeLuotScanCuaDungTram()
    {
        var t0 = DateTime.UtcNow;
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0),
            MakeScan(2, "B", workStationId: 2, lineId: 1, scannedAtUtc: t0.AddMinutes(1)),
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { WorkStationId = 1 });

        Assert.Single(result.Items);
        Assert.Equal(1, result.Items[0].WorkStationId);
    }

    // AC3 — Lọc riêng theo Line.
    [Fact]
    public async Task GetHistoryAsync_LocTheoLine_ChiTraVeLuotScanCuaDungLine()
    {
        var t0 = DateTime.UtcNow;
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0),
            MakeScan(2, "B", workStationId: 2, lineId: 2, scannedAtUtc: t0.AddMinutes(1)),
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { LineId = 2 });

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].LineId);
    }

    // Nâng cấp UX drill-down (19/08/2026) — Lọc riêng theo kết quả scan (Result).
    [Fact]
    public async Task GetHistoryAsync_LocTheoKetQuaScan_ChiTraVeLuotScanDungKetQua()
    {
        var t0 = DateTime.UtcNow;
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0, result: ScanResult.Ok),
            MakeScan(2, "B", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(1), result: ScanResult.Ng),
            MakeScan(3, "C", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(2), result: ScanResult.Ng),
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Result = ScanResult.Ng });

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(ScanResult.Ng, item.Result));
    }

    // Nâng cấp UX drill-down (19/08/2026) — Kết hợp filter Result với filter khác (AND).
    [Fact]
    public async Task GetHistoryAsync_KetHopFilterKetQuaVoiFilterKhac_TraVeDungGiaoCuaCacDieuKien()
    {
        var t0 = DateTime.UtcNow;
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0, result: ScanResult.Ok), // đúng trạm, sai kết quả
            MakeScan(2, "B", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(1), result: ScanResult.Ng), // đúng cả 2
            MakeScan(3, "C", workStationId: 2, lineId: 1, scannedAtUtc: t0.AddMinutes(2), result: ScanResult.Ng), // sai trạm, đúng kết quả
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { WorkStationId = 1, Result = ScanResult.Ng });

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }

    // AC3 — Lọc riêng theo khoảng thời gian (from/to).
    [Fact]
    public async Task GetHistoryAsync_LocTheoKhoangThoiGian_ChiTraVeLuotScanTrongKhoang()
    {
        var t0 = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0), // trước khoảng
            MakeScan(2, "B", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddHours(1)), // trong khoảng
            MakeScan(3, "C", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddHours(5)), // sau khoảng
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery
        {
            FromUtc = t0.AddMinutes(30),
            ToUtc = t0.AddHours(2),
        });

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }

    // AC3 — Kết hợp nhiều filter cùng lúc (AND): trạm + khoảng thời gian.
    [Fact]
    public async Task GetHistoryAsync_KetHopNhieuFilterCungLuc_TraVeDungGiaoCuaCacDieuKien()
    {
        var t0 = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        SetupExistingScans(new List<Scan>
        {
            MakeScan(1, "A", workStationId: 1, lineId: 1, scannedAtUtc: t0), // đúng trạm, sai thời gian
            MakeScan(2, "B", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddHours(1)), // đúng cả 2
            MakeScan(3, "C", workStationId: 2, lineId: 1, scannedAtUtc: t0.AddHours(1)), // sai trạm, đúng thời gian
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery
        {
            WorkStationId = 1,
            FromUtc = t0.AddMinutes(30),
            ToUtc = t0.AddHours(2),
        });

        Assert.Single(result.Items);
        Assert.Equal(2, result.Items[0].Id);
    }

    // AC3 — Phân trang: trả đúng số bản ghi/trang + TotalCount đúng tổng số phù hợp filter (không phải số trong trang).
    [Fact]
    public async Task GetHistoryAsync_PhanTrang_TraVeDungSoLuongMoiTrangVaTotalCountDungTongSo()
    {
        var t0 = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var scans = Enumerable.Range(1, 25)
            .Select(i => MakeScan(i, $"TAG{i}", workStationId: 1, lineId: 1, scannedAtUtc: t0.AddMinutes(i)))
            .ToList();
        SetupExistingScans(scans);

        var page1 = await _sut.GetHistoryAsync(new ScanHistoryQuery { Page = 1, PageSize = 10 });
        var page3 = await _sut.GetHistoryAsync(new ScanHistoryQuery { Page = 3, PageSize = 10 });

        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(10, page1.Items.Count);
        Assert.Equal(1, page1.Page);
        Assert.Equal(1, page1.Items[0].Id); // trang 1: bản ghi sớm nhất trước

        Assert.Equal(25, page3.TotalCount);
        Assert.Equal(5, page3.Items.Count); // trang cuối chỉ còn 5 bản ghi (25 - 20)
        Assert.Equal(21, page3.Items[0].Id);
    }

    // Page/PageSize không hợp lệ (< 1) tự chỉnh về mặc định thay vì lỗi/crash.
    [Fact]
    public async Task GetHistoryAsync_PageVaPageSizeKhongHopLe_TuChinhVeMacDinh()
    {
        SetupExistingScans(Enumerable.Range(1, 3)
            .Select(i => MakeScan(i, $"TAG{i}", workStationId: 1, lineId: 1, scannedAtUtc: DateTime.UtcNow.AddMinutes(i)))
            .ToList());

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Page = 0, PageSize = -5 });

        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize); // mặc định
        Assert.Equal(3, result.Items.Count);
    }

    // AC4 — Snapshot bất biến: lịch sử scan trả về đúng giá trị Customer/Model/Lot/Revision/PlannedQuantity/
    // TaktTimeSeconds/OperatorNames đã lưu tại thời điểm scan, không phải giá trị hiện tại của ProductionPlan (test
    // qua chính API tra cứu, không chỉ test lúc tạo scan). OperatorNames bổ sung 19/08/2026 (US-10 AC1/AC5).
    [Fact]
    public async Task GetHistoryAsync_TraCuuLaiLuotScanCu_TraVeDungSnapshotTaiThoiDiemScan_KhongPhaiGiaTriHienTaiCuaKeHoach()
    {
        var scanCu = new Scan
        {
            Id = 1,
            TagCode = "SNAP1",
            WorkStationId = 1,
            LineId = 1,
            StageId = 100,
            ProductionPlanId = 1,
            ScannedAtUtc = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc),
            Result = ScanResult.Ok,
            // Snapshot LƯU TẠI THỜI ĐIỂM SCAN (Số lượng = 1000, Takt time = 30s) — US-10 AC4.
            Customer = "Khách hàng A",
            Model = "Model X",
            Lot = "LOT001",
            Revision = "B",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30m,
            OperatorNames = "Nguyễn Văn A, Trần Thị B",
        };
        SetupExistingScans(new List<Scan> { scanCu });

        // Giả lập: sau đó Tổ trưởng đã sửa ProductionPlan (PlannedQuantity 1000 -> 1200) — nhưng KHÔNG có tác động
        // gì tới bản ghi Scan đã lưu (không có mock IRepository<ProductionPlan> nào ở đây vì GetHistoryAsync
        // không được phép tra cứu động qua ProductionPlanId, chỉ đọc thẳng field snapshot trên Scan).

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { TagCode = "SNAP1" });

        var item = Assert.Single(result.Items);
        Assert.Equal("Khách hàng A", item.Customer);
        Assert.Equal("Model X", item.Model);
        Assert.Equal("LOT001", item.Lot);
        Assert.Equal("B", item.Revision);
        Assert.Equal(1000, item.PlannedQuantity); // vẫn 1000 (giá trị lúc scan), không phải 1200 hiện tại
        Assert.Equal(30m, item.TaktTimeSeconds);
        Assert.Equal("Nguyễn Văn A, Trần Thị B", item.OperatorNames);
    }
}
