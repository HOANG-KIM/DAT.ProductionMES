using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.PackingBoxes;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ScanService phần máy thông điện (US-11 AC1/AC6, US-12): scan thường tại trạm không có máy / có máy +
/// công tắc TẮT (Bypass) / có máy + công tắc BẬT (bị chặn), endpoint kiểm tra không lưu, lưu theo kết quả máy + idempotent.
/// </summary>
public class ScanServiceTesterModeTests
{
    private const int StageId = 200;
    private const int WorkStationId = 2;

    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<ReworkUnlock>> _reworkUnlockRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductionPlanStageService> _productionPlanStageServiceMock = new();
    private readonly Mock<IScanNotifier> _scanNotifierMock = new();
    private readonly ScanService _sut;

    private readonly List<Scan> _existingScans = new();

    public ScanServiceTesterModeTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object, Mock.Of<IPackingBoxService>());

        _stageRepositoryMock.Setup(r => r.GetByIdAsync(StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = StageId, Name = "Thông điện", IsActive = true });

        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) => _existingScans.Where(predicate.Compile()).ToList());
        _scanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()))
            .Callback((Scan scan, CancellationToken _) => _existingScans.Add(scan))
            .Returns(Task.CompletedTask);

        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReworkUnlock>());

        var runningPlanStage = new ProductionPlanStage { Id = 1, ProductionPlanId = 2, StageId = StageId, LineId = 2, PlanStatus = PlanStatus.Running };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { runningPlanStage }.Where(predicate.Compile()).ToList());
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = 2, LineId = 2, PlannedQuantity = 1_000_000, Model = "M1", Lot = "L1", Customer = "C" });

        // Công đoạn đầu tiên (không có công đoạn liền trước) để các test tập trung vào chế độ máy thông điện.
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 2, StageId = StageId, SequenceNumber = 1, PreviousStageId = null },
            });
    }

    private void SetupWorkStation(bool useArduino, bool testerEnabled = true)
        => _workStationRepositoryMock.Setup(r => r.GetByIdAsync(WorkStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = WorkStationId, LineId = 2, StageId = StageId, Name = "Trạm TĐ", UseArduino = useArduino, TesterEnabled = testerEnabled });

    // US-11 AC1 — trạm không có máy: luồng thường, TesterMode = None.
    [Fact]
    public async Task CreateAsync_TramKhongCoMay_LuuOkVoiTesterModeNone()
    {
        SetupWorkStation(useArduino: false);

        var result = await _sut.CreateAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Equal(TesterMode.None, result.TesterMode);
        Assert.Equal(TesterMode.None, Assert.Single(_existingScans).TesterMode);
    }

    // US-11 AC6 — có máy + công tắc TẮT: vẫn lưu Result = Ok, đánh dấu Bypass, tính là đã qua công đoạn.
    [Fact]
    public async Task CreateAsync_TramCoMayCongTacTat_LuuOkVoiTesterModeBypass()
    {
        SetupWorkStation(useArduino: true, testerEnabled: false);

        var result = await _sut.CreateAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Equal(TesterMode.Bypass, result.TesterMode);
        var saved = Assert.Single(_existingScans);
        Assert.Equal(ScanResult.Ok, saved.Result);
        Assert.Equal(TesterMode.Bypass, saved.TesterMode);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(WorkStationId, It.Is<ScanResultDto>(d => d.Result == ScanResult.Ok), It.IsAny<CancellationToken>()), Times.Once);
    }

    // US-11 AC6 — Bypass vẫn qua kiểm tra FR-08: tem đã OK ở công đoạn này bị từ chối, không lưu.
    [Fact]
    public async Task CreateAsync_BypassTemDaOkTaiCongDoan_TuChoiDuplicateKhongLuu()
    {
        SetupWorkStation(useArduino: true, testerEnabled: false);
        _existingScans.Add(new Scan { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ok });

        var result = await _sut.CreateAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.Single(_existingScans);
    }

    // US-11 AC2 — có máy + công tắc BẬT: scan thường bị chặn (phải đi luồng máy), không lưu gì.
    [Fact]
    public async Task CreateAsync_TramCoMayCongTacBat_ChanScanThuong()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(WorkStationId, "TAG1"));

        Assert.Empty(_existingScans);
    }
}
