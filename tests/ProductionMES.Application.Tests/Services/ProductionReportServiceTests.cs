using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="ProductionReportService"/> (US-21) — tập trung vào orchestration (gom đúng cặp
/// (Line, Công đoạn) từ trạm đang hoạt động, chọn đúng kế hoạch tham chiếu theo chế độ thời gian thực/khoảng thời
/// gian đã qua, AC3 chỉ tính Ok). Công thức PLAN trừ khung giờ nghỉ đã kiểm thử đầy đủ ở
/// <see cref="AndonBoardCalculatorTests"/> — ở đây chỉ xác nhận 1 test wiring đại diện.
/// </summary>
public class ProductionReportServiceTests
{
    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<BreakWindow>> _breakWindowRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProductionReportService _sut;

    public ProductionReportServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<BreakWindow>()).Returns(_breakWindowRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);

        _sut = new ProductionReportService(_unitOfWorkMock.Object);

        SetupWorkStations(new List<WorkStation>());
        SetupLines(new List<Line>());
        SetupStages(new List<Stage>());
        SetupBreakWindows(new List<BreakWindow>());
        SetupPlanStages(new List<ProductionPlanStage>());
        SetupPlans(new List<ProductionPlan>());
        SetupOkScans(new List<Scan>());
    }

    private void SetupWorkStations(List<WorkStation> workStations) =>
        _workStationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<WorkStation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<WorkStation, bool>> predicate, CancellationToken _) => workStations.Where(predicate.Compile()).ToList());

    private void SetupLines(List<Line> lines) =>
        _lineRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Line, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Line, bool>> predicate, CancellationToken _) => lines.Where(predicate.Compile()).ToList());

    private void SetupStages(List<Stage> stages) =>
        _stageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Stage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Stage, bool>> predicate, CancellationToken _) => stages.Where(predicate.Compile()).ToList());

    private void SetupBreakWindows(List<BreakWindow> breakWindows) =>
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<BreakWindow, bool>> predicate, CancellationToken _) => breakWindows.Where(predicate.Compile()).ToList());

    private void SetupPlanStages(List<ProductionPlanStage> planStages) =>
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) => planStages.Where(predicate.Compile()).ToList());

    private void SetupPlans(List<ProductionPlan> plans) =>
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlan, bool>> predicate, CancellationToken _) => plans.Where(predicate.Compile()).ToList());

    private void SetupOkScans(List<Scan> scans) =>
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) => scans.Where(predicate.Compile()).ToList());

    // Không có trạm nào đang hoạt động -> báo cáo rỗng, không lỗi.
    [Fact]
    public async Task GetReportAsync_KhongCoTramNaoHoatDong_TraVeRowsRong()
    {
        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        Assert.Empty(result.Rows);
        Assert.Null(result.FromUtc);
        Assert.Null(result.ToUtc);
    }

    // AC1: không truyền from/to -> dùng kế hoạch đang Running, PLAN/ACTUAL tính lũy kế từ StartTime tới hiện tại.
    [Fact]
    public async Task GetReportAsync_ThoiGianThuc_CoKeHoachDangRunning_TinhDungActualPlanBalance()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var startTime = DateTime.Now.AddHours(-3);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, StartTime = startTime, TaktTimeSeconds = 3600m } }); // 1 sp/giờ

        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "B", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.LineId);
        Assert.Equal("Line 1", row.LineName);
        Assert.Equal(100, row.StageId);
        Assert.Equal("Lắp ráp", row.StageName);
        Assert.True(row.HasPlanData);
        Assert.Equal(new[] { 10 }, row.ProductionPlanIds);
        Assert.Equal(2, row.Actual);
        Assert.Equal(3, row.Plan); // 3 giờ trôi qua x 1 sp/giờ
        Assert.Equal(row.Actual - row.Plan, row.Balance);
    }

    // AC1: (Line, Công đoạn) chưa/không còn kế hoạch nào Running -> HasPlanData = false, mọi số liệu = 0 (không lỗi).
    [Fact]
    public async Task GetReportAsync_ThoiGianThuc_KhongCoKeHoachRunning_TraVeHasPlanDataFalse()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.False(row.HasPlanData);
        Assert.Empty(row.ProductionPlanIds);
        Assert.Equal(0, row.Actual);
        Assert.Equal(0, row.Plan);
        Assert.Equal(0, row.Balance);
    }

    // Nhiều cặp (Line, Công đoạn) cùng lúc -> mỗi dòng tính riêng, không lẫn dữ liệu của nhau.
    [Fact]
    public async Task GetReportAsync_NhieuCapLineCongDoan_TinhRiengTungDongKhongLan()
    {
        SetupWorkStations(new List<WorkStation>
        {
            new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" },
            new() { Id = 2, LineId = 2, StageId = 200, IsActive = true, Name = "Trạm 2" },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" }, new() { Id = 200, Name = "Đóng gói" } });

        var startTime = DateTime.Now.AddHours(-2);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 200, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, StartTime = startTime, TaktTimeSeconds = 3600m },
            new() { Id = 20, LineId = 2, StartTime = startTime, TaktTimeSeconds = 1800m }, // 2 sp/giờ
        });
        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "B", ProductionPlanId = 20, StageId = 200, LineId = 2, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "C", ProductionPlanId = 20, StageId = 200, LineId = 2, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        Assert.Equal(2, result.Rows.Count);
        var row1 = result.Rows.Single(r => r.LineId == 1);
        var row2 = result.Rows.Single(r => r.LineId == 2);
        Assert.Equal(1, row1.Actual);
        Assert.Equal(2, row1.Plan); // 2 giờ x 1 sp/giờ
        Assert.Equal(2, row2.Actual);
        Assert.Equal(4, row2.Plan); // 2 giờ x 2 sp/giờ
    }

    // LineId filter: chỉ trả về dòng thuộc đúng Line được lọc.
    [Fact]
    public async Task GetReportAsync_LocTheoLineId_ChiTraVeDongCuaDungLineDo()
    {
        SetupWorkStations(new List<WorkStation>
        {
            new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" },
            new() { Id = 2, LineId = 2, StageId = 200, IsActive = true, Name = "Trạm 2" },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" }, new() { Id = 200, Name = "Đóng gói" } });

        var result = await _sut.GetReportAsync(new ProductionReportQuery { LineId = 1 });

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.LineId);
    }

    // Trạm đã bị vô hiệu hóa (IsActive = false) không xuất hiện trong báo cáo.
    [Fact]
    public async Task GetReportAsync_TramDaVoHieuHoa_KhongXuatHienTrongBaoCao()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = false, Name = "Trạm cũ" } });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        Assert.Empty(result.Rows);
    }

    // AC3: chỉ ScanResult.Ok mới được tính vào ACTUAL — Ng/DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock
    // không được truyền vào repository Scan (Service tự lọc Result == Ok khi query), nên test này xác nhận đúng
    // hành vi filter đó bằng cách dựng sẵn tập scan hỗn hợp và kiểm tra chỉ Ok được đếm.
    [Fact]
    public async Task GetReportAsync_TronLanNhieuKetQuaScan_AC3_ChiTinhOkVaoActual()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var startTime = DateTime.Now.AddHours(-1);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, StartTime = startTime, TaktTimeSeconds = 3600m } });

        // Service tự truyền Result == Ok vào predicate FindAsync<Scan> -> mock chỉ nhận về đúng phần thỏa mãn.
        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "B", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "C", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.DuplicateTag, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "D", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.PreviousStageNotPassed, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "E", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.WaitingReworkUnlock, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.Actual);
    }

    // AC2: có truyền from/to -> ACTUAL chỉ đếm trong khoảng, PLAN = hiệu số PlanCumulative(to) - PlanCumulative(from);
    // dùng kế hoạch có StartTime gần nhất tính tới "to", KHÔNG bắt buộc đang Running (ở đây đã Completed).
    [Fact]
    public async Task GetReportAsync_KhoangThoiGianDaQua_AC2_TinhDungActualVaPlanTrongKhoang()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var planStart = new DateTime(2026, 8, 10, 8, 0, 0);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            // Đã Completed (khác Running) -> AC2 vẫn phải dùng được, vì đây là khoảng thời gian ĐÃ QUA.
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Completed },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, StartTime = planStart, TaktTimeSeconds = 3600m } }); // 1 sp/giờ

        // Đổi ý 19/08/2026: FromUtc/ToUtc và Scan.ScannedAtUtc nay đều là giờ local, KHÔNG quy đổi UTC nữa — bỏ
        // .ToUniversalTime() để cùng hệ quy chiếu với planStart (xem API-Conventions.md mục 10).
        var fromUtc = planStart.AddHours(1); // 09:00
        var toUtc = planStart.AddHours(4); // 12:00

        SetupOkScans(new List<Scan>
        {
            // Trước khoảng lọc -> không tính vào Actual.
            new() { TagCode = "Before", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddMinutes(30) },
            // Trong khoảng [from, to] -> tính vào Actual.
            new() { TagCode = "In1", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddHours(2) },
            new() { TagCode = "In2", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddHours(3) },
            // Sau khoảng lọc -> không tính vào Actual.
            new() { TagCode = "After", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddHours(5) },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery { FromUtc = fromUtc, ToUtc = toUtc });

        var row = Assert.Single(result.Rows);
        Assert.True(row.HasPlanData);
        Assert.Equal(new[] { 10 }, row.ProductionPlanIds);
        Assert.Equal(2, row.Actual); // In1, In2
        Assert.Equal(3, row.Plan); // PlanCumulative(12:00)=4 - PlanCumulative(09:00)=1 => 3
        Assert.Equal(row.Actual - row.Plan, row.Balance);
        Assert.Equal(fromUtc, result.FromUtc);
        Assert.Equal(toUtc, result.ToUtc);
    }

    // AC5/AC6 wiring: khung giờ nghỉ đúng Line phải được nạp và trừ vào PLAN (công thức đầy đủ đã test ở AndonBoardCalculatorTests).
    [Fact]
    public async Task GetReportAsync_CoKhungGioNghiCuaDungLine_TruDungVaoPlan()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var now = DateTime.Now;
        var startTime = now.AddHours(-3);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, StartTime = startTime, TaktTimeSeconds = 3600m } });

        var withoutBreak = await _sut.GetReportAsync(new ProductionReportQuery());
        Assert.Equal(3, withoutBreak.Rows.Single().Plan);

        SetupBreakWindows(new List<BreakWindow>
        {
            new() { LineId = 1, StartTime = TimeOnly.FromDateTime(now.AddMinutes(-90)), EndTime = TimeOnly.FromDateTime(now.AddMinutes(-30)) },
            new() { LineId = 2, StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(0, 1) },
        });

        var withBreak = await _sut.GetReportAsync(new ProductionReportQuery());
        Assert.Equal(2, withBreak.Rows.Single().Plan);
    }

    // AC4 (mới): cùng (Line, Công đoạn) nhưng 2 Lot khác nhau -> 2 dòng riêng biệt, không gộp chung.
    [Fact]
    public async Task GetReportAsync_AC4_2LotKhacNhauCungLineCongDoan_TachThanh2Dong()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var planStart = new DateTime(2026, 8, 10, 8, 0, 0);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Completed },
            new() { Id = 2, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Lot = "LOT-A", StartTime = planStart, TaktTimeSeconds = 3600m },
            new() { Id = 11, LineId = 1, Lot = "LOT-B", StartTime = planStart.AddHours(2), TaktTimeSeconds = 3600m },
        });
        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddHours(1).ToUniversalTime() },
            new() { TagCode = "B", ProductionPlanId = 11, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = planStart.AddHours(3).ToUniversalTime() },
        });

        var fromUtc = planStart.ToUniversalTime();
        var toUtc = planStart.AddHours(4).ToUniversalTime();
        var result = await _sut.GetReportAsync(new ProductionReportQuery { FromUtc = fromUtc, ToUtc = toUtc });

        Assert.Equal(2, result.Rows.Count);
        var rowA = result.Rows.Single(r => r.Lot == "LOT-A");
        var rowB = result.Rows.Single(r => r.Lot == "LOT-B");
        Assert.Equal(new[] { 10 }, rowA.ProductionPlanIds);
        Assert.Equal(1, rowA.Actual);
        Assert.Equal(new[] { 11 }, rowB.ProductionPlanIds);
        Assert.Equal(1, rowB.Actual);
    }

    // AC5 (mới): NgCount chỉ đếm Result = Ng, KHÔNG tính DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock.
    [Fact]
    public async Task GetReportAsync_AC5_NgCountChiDemResultNg()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var startTime = DateTime.Now.AddHours(-1);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Lot = "LOT-A", StartTime = startTime, TaktTimeSeconds = 3600m } });

        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "B", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "C", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "D", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.DuplicateTag, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "E", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.PreviousStageNotPassed, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "F", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.WaitingReworkUnlock, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.Actual);
        Assert.Equal(2, row.NgCount);
    }

    // Quyết định 18/08/2026: cùng (Line, Công đoạn, Lot) nhưng khác ProductionPlanId (Lot bị tạo lại thành kế
    // hoạch mới sau khi kế hoạch cũ Cancelled) -> GỘP CHUNG thành 1 dòng, cộng dồn Actual/NgCount/Plan.
    [Fact]
    public async Task GetReportAsync_CungLotKhacProductionPlanId_GopChungThanh1Dong()
    {
        SetupWorkStations(new List<WorkStation> { new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" } });

        var oldPlanStart = DateTime.Now.AddHours(-5);
        var newPlanStart = DateTime.Now.AddHours(-2);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
            new() { Id = 2, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Lot = "LOT-X", StartTime = oldPlanStart, TaktTimeSeconds = 3600m },
            new() { Id = 11, LineId = 1, Lot = "LOT-X", StartTime = newPlanStart, TaktTimeSeconds = 3600m },
        });
        SetupOkScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = oldPlanStart.AddMinutes(30).ToUniversalTime() },
            new() { TagCode = "B", ProductionPlanId = 10, StageId = 100, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = oldPlanStart.AddMinutes(40).ToUniversalTime() },
            new() { TagCode = "C", ProductionPlanId = 11, StageId = 100, LineId = 1, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal("LOT-X", row.Lot);
        Assert.Equal(new[] { 10, 11 }, row.ProductionPlanIds.OrderBy(x => x));
        Assert.Equal(2, row.Actual); // A (plan cũ) + C (plan mới)
        Assert.Equal(1, row.NgCount); // B (plan cũ)
        Assert.Equal(2 + 5, row.Plan); // PLAN kế hoạch cũ (5 giờ x 1sp/h) + kế hoạch mới (2 giờ x 1sp/h)
    }

    // AC6 (mới): lọc theo Model — cặp (Line, Công đoạn) không khớp bị LOẠI HẲN, không hiển thị dòng "Chưa có kế hoạch".
    [Fact]
    public async Task GetReportAsync_AC6_LocTheoModel_LoaiHanCapKhongKhopKhoiKetQua()
    {
        SetupWorkStations(new List<WorkStation>
        {
            new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" },
            new() { Id = 2, LineId = 2, StageId = 200, IsActive = true, Name = "Trạm 2" },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" }, new() { Id = 200, Name = "Đóng gói" } });

        var startTime = DateTime.Now.AddHours(-1);
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 200, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Lot = "LOT-A", Model = "M1", StartTime = startTime, TaktTimeSeconds = 3600m },
            new() { Id = 20, LineId = 2, Lot = "LOT-B", Model = "M2", StartTime = startTime, TaktTimeSeconds = 3600m },
        });

        var result = await _sut.GetReportAsync(new ProductionReportQuery { Model = "M1" });

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.LineId);
        Assert.Equal("LOT-A", row.Lot);
    }

    // AC6 (mới): lọc theo StageId — chỉ trả về dòng của đúng công đoạn được lọc.
    [Fact]
    public async Task GetReportAsync_AC6_LocTheoStageId_ChiTraVeDongCuaDungCongDoan()
    {
        SetupWorkStations(new List<WorkStation>
        {
            new() { Id = 1, LineId = 1, StageId = 100, IsActive = true, Name = "Trạm 1" },
            new() { Id = 2, LineId = 1, StageId = 200, IsActive = true, Name = "Trạm 2" },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp" }, new() { Id = 200, Name = "Đóng gói" } });

        var result = await _sut.GetReportAsync(new ProductionReportQuery { StageId = 100 });

        var row = Assert.Single(result.Rows);
        Assert.Equal(100, row.StageId);
    }
}
