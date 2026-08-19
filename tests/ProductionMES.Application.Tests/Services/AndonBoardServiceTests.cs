using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Lots;
using ProductionMES.Application.Services.AndonBoard;
using ProductionMES.Application.Services.Lots;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="AndonBoardService"/> (US-09) — tập trung vào orchestration (nạp đúng dữ liệu, lọc
/// đúng theo ProductionPlanId/StageId, HasActivePlan, NG/%NG). Logic tính toán PLAN theo mốc giờ có trừ khung
/// giờ nghỉ (AC5/AC6) đã được kiểm thử đầy đủ ở <see cref="AndonBoardCalculatorTests"/> — ở đây chỉ xác nhận
/// service NẠP ĐÚNG khung giờ nghỉ của đúng Line và truyền vào phép tính (1 test đại diện), không lặp lại toàn
/// bộ ma trận edge case đã cover ở đó.
/// </summary>
public class AndonBoardServiceTests
{
    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<BreakWindow>> _breakWindowRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILotService> _lotServiceMock = new();
    private readonly AndonBoardService _sut;

    public AndonBoardServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<BreakWindow>()).Returns(_breakWindowRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);

        _sut = new AndonBoardService(_unitOfWorkMock.Object, _lotServiceMock.Object);

        SetupExistingBreakWindows(new List<BreakWindow>());
        SetupExistingScans(new List<Scan>());
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());
        // US-21a AC8: mặc định Lot CHƯA XÁC ĐỊNH (chưa từng có ai nhập) — test riêng override qua _lotServiceMock.
        _lotServiceMock.Setup(s => s.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((LotDto?)null);
    }

    private void SetupExistingBreakWindows(List<BreakWindow> breakWindows)
    {
        _breakWindowRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<BreakWindow, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<BreakWindow, bool>> predicate, CancellationToken _) =>
                breakWindows.Where(predicate.Compile()).ToList());
    }

    private void SetupExistingScans(List<Scan> scans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                scans.Where(predicate.Compile()).ToList());
    }

    private void SetupRunningPlan(
        int workStationId, int lineId, int stageId, int productionPlanId, DateTime startTime, decimal taktTimeSeconds)
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(workStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = workStationId, LineId = lineId, StageId = stageId, Name = $"Trạm {workStationId}" });

        var runningPlanStage = new ProductionPlanStage
        {
            Id = 1, ProductionPlanId = productionPlanId, StageId = stageId, LineId = lineId, PlanStatus = PlanStatus.Running,
        };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { runningPlanStage }.Where(predicate.Compile()).ToList());

        var runningPlan = new ProductionPlan
        {
            Id = productionPlanId, LineId = lineId, StartTime = startTime, TaktTimeSeconds = taktTimeSeconds, PlannedQuantity = 1_000_000,
            Model = "33800-82M71/D", Lot = "LOT-01", OperatorNames = "HOANG, MINH, TUAN",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(productionPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(runningPlan);
    }

    // Phòng vệ: trạm không tồn tại -> EntityNotFoundException, không NullReference.
    [Fact]
    public async Task GetForWorkStationAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetForWorkStationAsync(999));
    }

    // Khác ScanService (409 khi không có kế hoạch Running) — endpoint này CHỈ HIỂN THỊ nên trả về HasActivePlan = false, không lỗi.
    [Fact]
    public async Task GetForWorkStationAsync_KhongCoKeHoachNaoRunning_TraVeHasActivePlanFalse()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = 100, Name = "Trạm chưa có kế hoạch" });

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.False(result.HasActivePlan);
        Assert.Null(result.ProductionPlanId);
        Assert.Empty(result.Rows);
        Assert.Equal(0, result.ActualCumulative);
        Assert.Equal(0, result.PlanCumulative);
        // Không có kế hoạch active -> các field header bổ sung (mockup 13/08/2026) giữ giá trị mặc định, không lỗi.
        Assert.Equal(string.Empty, result.Model);
        Assert.Equal(string.Empty, result.Lot);
        Assert.Equal(0, result.PlannedQuantity);
        Assert.Equal(0m, result.TaktTimeSeconds);
        Assert.Null(result.PlanStartTime);
        Assert.Equal(string.Empty, result.OperatorNames);
    }

    // AC1: ACTUAL/NG chỉ tính đúng cặp (ProductionPlanId, StageId) của kế hoạch active, bỏ qua scan của công đoạn/kế hoạch khác.
    // US-18 (gap đã sửa): NG chỉ tính ScanResult.Ng (lỗi chất lượng, xác nhận thủ công) — KHÔNG tính
    // DuplicateTag/PreviousStageNotPassed (lỗi thao tác, không phải lỗi chất lượng sản phẩm).
    [Fact]
    public async Task GetForWorkStationAsync_CoKeHoachRunning_TinhDungActualVaNgTheoDungKeHoachVaCongDoan()
    {
        var startTime = DateTime.Now.AddHours(-5);
        SetupRunningPlan(workStationId: 1, lineId: 1, stageId: 100, productionPlanId: 10, startTime, taktTimeSeconds: 3600m);

        SetupExistingScans(new List<Scan>
        {
            new() { TagCode = "A", ProductionPlanId = 10, StageId = 100, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "B", ProductionPlanId = 10, StageId = 100, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { TagCode = "C", ProductionPlanId = 10, StageId = 100, Result = ScanResult.Ng, RejectionReason = "Trầy xước vỏ", ScannedAtUtc = DateTime.UtcNow },
            // Lỗi thao tác (không phải lỗi chất lượng) -> KHÔNG tính vào NG/%NG dù cùng kế hoạch/công đoạn.
            new() { TagCode = "F", ProductionPlanId = 10, StageId = 100, Result = ScanResult.DuplicateTag, ScannedAtUtc = DateTime.UtcNow },
            // Cùng kế hoạch nhưng khác công đoạn -> KHÔNG tính vào.
            new() { TagCode = "D", ProductionPlanId = 10, StageId = 200, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            // Khác kế hoạch -> KHÔNG tính vào.
            new() { TagCode = "E", ProductionPlanId = 99, StageId = 100, Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.True(result.HasActivePlan);
        Assert.Equal(10, result.ProductionPlanId);
        // Header bổ sung theo mockup 13/08/2026: map thẳng từ ProductionPlan, không tính toán gì thêm.
        Assert.Equal("33800-82M71/D", result.Model);
        Assert.Equal("LOT-01", result.Lot);
        Assert.Equal(1_000_000, result.PlannedQuantity);
        Assert.Equal(3600m, result.TaktTimeSeconds);
        Assert.Equal(startTime, result.PlanStartTime);
        Assert.Equal("HOANG, MINH, TUAN", result.OperatorNames);
        Assert.Equal(2, result.ActualCumulative); // A, B
        Assert.Equal(1, result.NgCount); // C (F là DuplicateTag, không tính)
        Assert.Equal(Math.Round(1 * 100m / 3, 1), result.NgPercent); // 1 NG / (2 OK + 1 NG) = 33.3% — F không vào mẫu số
        Assert.Equal(result.ActualCumulative - result.PlanCumulative, result.Balance);

        // AC6: dòng cuối cùng luôn là dòng "hiện tại".
        Assert.NotEmpty(result.Rows);
        Assert.True(result.Rows[^1].IsCurrent);
        Assert.All(result.Rows.Take(result.Rows.Count - 1), row => Assert.False(row.IsCurrent));
        Assert.Equal(result.PlanCumulative, result.Rows[^1].PlanCumulative);
        Assert.Equal(result.ActualCumulative, result.Rows[^1].ActualCumulative);
    }

    // Chưa có lượt scan nào -> %NG = 0 (không chia cho 0).
    [Fact]
    public async Task GetForWorkStationAsync_ChuaCoLuotScanNao_NgPercentBang0()
    {
        SetupRunningPlan(workStationId: 1, lineId: 1, stageId: 100, productionPlanId: 10, DateTime.Now.AddHours(-1), taktTimeSeconds: 60m);

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.Equal(0, result.NgCount);
        Assert.Equal(0, result.ActualCumulative);
        Assert.Equal(0m, result.NgPercent);
    }

    // AC5/AC6 (wiring): khung giờ nghỉ ĐÚNG Line của trạm phải được nạp và ảnh hưởng tới PLAN lũy kế — so sánh
    // có/không có khung giờ nghỉ bao trọn 1 giờ nằm giữa khoảng [StartTime, now] (đủ dài để không bị nhiễu bởi
    // vài mili-giây trôi qua giữa 2 lần gọi service trong cùng 1 test).
    [Fact]
    public async Task GetForWorkStationAsync_CoKhungGioNghiCuaDungLine_TruDungVaoPlanCumulative()
    {
        var now = DateTime.Now;
        var startTime = now.AddHours(-3);
        SetupRunningPlan(workStationId: 1, lineId: 1, stageId: 100, productionPlanId: 10, startTime, taktTimeSeconds: 3600m); // 1 sản phẩm/giờ

        var withoutBreak = await _sut.GetForWorkStationAsync(1);
        Assert.Equal(3, withoutBreak.PlanCumulative);

        // Khung giờ nghỉ 1 giờ, nằm gọn trong khoảng đã trôi qua (từ 1h30' tới 30' trước "now") -> Line 1 áp dụng, Line 2 (khác) không liên quan.
        SetupExistingBreakWindows(new List<BreakWindow>
        {
            new() { LineId = 1, StartTime = TimeOnly.FromDateTime(now.AddMinutes(-90)), EndTime = TimeOnly.FromDateTime(now.AddMinutes(-30)) },
            new() { LineId = 2, StartTime = new TimeOnly(0, 0), EndTime = new TimeOnly(0, 1) },
        });

        var withBreak = await _sut.GetForWorkStationAsync(1);

        Assert.Equal(2, withBreak.PlanCumulative);
    }

    // US-21a AC8 (viết lại hoàn toàn 19/08/2026): Lot CHƯA XÁC ĐỊNH (chưa từng có ai nhập "Tổng số lượng Lot") -> null (client tự ẩn ô).
    [Fact]
    public async Task GetForWorkStationAsync_LotChuaXacDinh_LotTotalQuantityNull()
    {
        var startTime = DateTime.Now.AddHours(-1);
        SetupRunningPlan(workStationId: 1, lineId: 1, stageId: 100, productionPlanId: 10, startTime, taktTimeSeconds: 60m);
        // Mặc định constructor: _lotServiceMock.GetByCodeAsync trả về null (chưa xác định).

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.Null(result.LotTotalQuantity);
    }

    // US-21a AC8: Lot ĐÃ có ai nhập "Tổng số lượng Lot" -> hiển thị đúng giá trị nhập tay (KHÔNG phải SUM/PlannedQuantity).
    [Fact]
    public async Task GetForWorkStationAsync_LotDaCoTongSoLuongNhapTay_HienThiDungGiaTri()
    {
        var startTime = DateTime.Now.AddHours(-1);
        SetupRunningPlan(workStationId: 1, lineId: 1, stageId: 100, productionPlanId: 10, startTime, taktTimeSeconds: 60m);
        _lotServiceMock
            .Setup(s => s.GetByCodeAsync("LOT-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LotDto { Code = "LOT-01", TotalQuantity = 2000 });

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.Equal(2000, result.LotTotalQuantity);
    }

    // Không có kế hoạch active -> LotTotalQuantity null (không truy vấn/tính gì thêm).
    [Fact]
    public async Task GetForWorkStationAsync_KhongCoKeHoachActive_LotTotalQuantityNull()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = 100, Name = "Trạm chưa có kế hoạch" });

        var result = await _sut.GetForWorkStationAsync(1);

        Assert.Null(result.LotTotalQuantity);
    }
}
