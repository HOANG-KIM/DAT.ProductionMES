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

    private static CreateTesterScanRequest TesterRequest(string tag = "TAG1", string boot = "8F21C04D", int seq = 105) => new()
    {
        TagCode = tag,
        WorkStationId = WorkStationId,
        TesterBootId = boot,
        TesterSeq = seq,
        GoldenCrc = "3A7F",
        FirmwareVersion = "1.0.0",
        TestedAt = new DateTime(2026, 10, 12, 8, 30, 0),
    };

    // US-12 AC3/AC4 — kiểm tra tem hợp lệ: trả Ok nhưng KHÔNG ghi gì, không bắn real-time.
    [Fact]
    public async Task CheckAsync_TemHopLe_TraOkKhongLuuKhongBanRealtime()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);

        var result = await _sut.CheckAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Empty(_existingScans);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-12 AC3 — dùng đúng logic FR-08: tem đã OK tại công đoạn -> DuplicateTag, vẫn không ghi gì.
    [Fact]
    public async Task CheckAsync_TemTrungTaiCongDoan_TraDuplicateTagKhongLuu()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        _existingScans.Add(new Scan { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ok });

        var result = await _sut.CheckAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.Single(_existingScans);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-12 AC3 — chưa qua công đoạn liền trước -> PreviousStageNotPassed.
    [Fact]
    public async Task CheckAsync_ChuaQuaCongDoanLienTruoc_TraPreviousStageNotPassed()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 2, StageId = 100, SequenceNumber = 1, PreviousStageId = null },
                new() { Id = 2, ProductionPlanId = 2, StageId = StageId, SequenceNumber = 2, PreviousStageId = 100 },
            });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = 100, Name = "Lắp ráp", IsActive = true });

        var result = await _sut.CheckAsync(WorkStationId, "TAG1");

        Assert.Equal(ScanResult.PreviousStageNotPassed, result.Result);
        Assert.Equal("Chưa qua công đoạn: Lắp ráp", result.RejectionReason);
    }

    // US-12 AC2 — lưu OK kèm đủ trường máy; bắn real-time (tính vào ACTUAL).
    [Fact]
    public async Task CreateTesterScanAsync_LuuOkKemDayDuTruongMay()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);

        var result = await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest());

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Equal(TesterMode.Tester, result.TesterMode);
        var saved = Assert.Single(_existingScans);
        Assert.Equal("TAG1", saved.TagCode);
        Assert.Equal(ScanResult.Ok, saved.Result);
        Assert.Equal(TesterMode.Tester, saved.TesterMode);
        Assert.Equal("8F21C04D", saved.TesterBootId);
        Assert.Equal(105, saved.TesterSeq);
        Assert.Equal("3A7F", saved.GoldenCrc);
        Assert.Equal("1.0.0", saved.FirmwareVersion);
        Assert.Equal(new DateTime(2026, 10, 12, 8, 30, 0), saved.TestedAtUtc);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(WorkStationId, It.Is<ScanResultDto>(d => d.Result == ScanResult.Ok), It.IsAny<CancellationToken>()), Times.Once);
    }

    // US-12 AC15 — cùng bộ khóa (retry): trả bản ghi cũ như thành công, không tạo thêm, không cộng ACTUAL lần 2.
    [Fact]
    public async Task CreateTesterScanAsync_RetryCungBoKhoa_TraBanGhiCuKhongTaoMoi()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest());

        var retry = await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest());

        Assert.Equal(ScanResult.Ok, retry.Result);
        Assert.Equal("TAG1", retry.TagCode);
        Assert.Single(_existingScans);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Once);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // US-12 AC15 — cùng bộ khóa nhưng tem khác: không thể gắn 1 kết quả máy cho 2 tem.
    [Fact]
    public async Task CreateTesterScanAsync_CungBoKhoaKhacTem_NemBusinessRuleException()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest(tag: "TAG1"));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateTesterScanAsync(WorkStationId, TesterRequest(tag: "TAG2")));

        Assert.Single(_existingScans);
    }

    // BOOT khác (máy đã reset) -> SEQ trùng số nhưng là kết quả khác, được lưu bình thường.
    [Fact]
    public async Task CreateTesterScanAsync_CungSeqKhacBoot_LuuBanGhiMoi()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest(tag: "TAG1", boot: "AAAAAAAA", seq: 1));

        await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest(tag: "TAG2", boot: "BBBBBBBB", seq: 1));

        Assert.Equal(2, _existingScans.Count);
    }

    // Tem đã bị chiếm giữa lúc kiểm tra và lúc lưu -> từ chối, không lưu.
    [Fact]
    public async Task CreateTesterScanAsync_TemDaOkTaiCongDoan_TuChoiKhongLuu()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        _existingScans.Add(new Scan { TagCode = "TAG1", StageId = StageId, WorkStationId = 9, Result = ScanResult.Ok });

        var result = await _sut.CreateTesterScanAsync(WorkStationId, TesterRequest());

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.Single(_existingScans);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTesterScanAsync_TramKhongCoMay_NemBusinessRuleException()
    {
        SetupWorkStation(useArduino: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateTesterScanAsync(WorkStationId, TesterRequest()));
    }

    [Fact]
    public async Task CreateTesterScanAsync_CongTacTat_NemBusinessRuleException()
    {
        SetupWorkStation(useArduino: true, testerEnabled: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateTesterScanAsync(WorkStationId, TesterRequest()));
        Assert.Empty(_existingScans);
    }
}
