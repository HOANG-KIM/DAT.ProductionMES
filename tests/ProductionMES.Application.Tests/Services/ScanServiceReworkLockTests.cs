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

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho bước kiểm tra "đang khóa rework" mới bổ sung vào <see cref="ScanService.CreateAsync"/> (US-19
/// AC1/AC3/AC5) — tách riêng khỏi <see cref="ScanServiceTests"/> (US-08) cho dễ đọc, cùng cách mock repository.
/// </summary>
public class ScanServiceReworkLockTests
{
    private const int StageId = 100;

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

    private List<Scan> _existingScans = new();
    private List<ReworkUnlock> _existingUnlocks = new();

    public ScanServiceReworkLockTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        // US-25: mặc định Stage KHÔNG phải "Đóng thùng" — không kích hoạt bước kiểm tra/đếm đặc thù trong luồng test này.
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(StageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = StageId, Name = "Công đoạn", IsActive = true });

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object, Mock.Of<IPackingBoxService>());

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = StageId, Name = "Trạm 1" });

        var runningPlanStage = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = StageId, LineId = 1, PlanStatus = PlanStatus.Running };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { runningPlanStage });

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = 1, LineId = 1, PlannedQuantity = 1_000_000 });

        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = StageId, SequenceNumber = 1, PreviousStageId = null },
            });

        SetupExistingScans(new List<Scan>());
        SetupExistingReworkUnlocks(new List<ReworkUnlock>());
    }

    private void SetupExistingScans(List<Scan> existingScans)
    {
        _existingScans = existingScans;
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                _existingScans.Where(predicate.Compile()).ToList());
        _scanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()))
            .Callback((Scan scan, CancellationToken _) => _existingScans.Add(scan))
            .Returns(Task.CompletedTask);
    }

    private void SetupExistingReworkUnlocks(List<ReworkUnlock> existingUnlocks)
    {
        _existingUnlocks = existingUnlocks;
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ReworkUnlock, bool>> predicate, CancellationToken _) =>
                _existingUnlocks.Where(predicate.Compile()).ToList());
    }

    // AC1 (AC-14 gốc): tem vừa bị Ng, chưa được Tổ trưởng mở khóa -> hệ thống TỰ ĐỘNG từ chối khi công nhân scan lại bình thường.
    [Fact]
    public async Task CreateAsync_TemDangBiKhoaReworkChuaMoKhoa_TuChoiWaitingReworkUnlock()
    {
        var ngAtUtc = DateTime.UtcNow.AddMinutes(-5);
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = ngAtUtc } });

        var result = await _sut.CreateAsync(1, "TAG1");

        Assert.Equal(ScanResult.WaitingReworkUnlock, result.Result);
        Assert.Equal("Sản phẩm đang chờ mở khóa rework.", result.RejectionReason);
        // FR-10: lượt scan bị từ chối vẫn được lưu lại đầy đủ lịch sử.
        _scanRepositoryMock.Verify(r => r.AddAsync(
            It.Is<Scan>(s => s.Result == ScanResult.WaitingReworkUnlock && s.TagCode == "TAG1"), It.IsAny<CancellationToken>()), Times.Once);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC3 (AC-15 gốc): Tổ trưởng đã mở khóa (ReworkUnlock SAU lượt Ng gần nhất) -> công nhân scan lại bình thường, đạt -> ghi Ok mới, KHÔNG ghi đè bản ghi Ng cũ.
    [Fact]
    public async Task CreateAsync_DaDuocMoKhoaReworkVaScanLaiDat_GhiNhanOkMoiKhongGhiDeNgCu()
    {
        var ngAtUtc = DateTime.UtcNow.AddMinutes(-10);
        var unlockAtUtc = DateTime.UtcNow.AddMinutes(-5);
        var ngScan = new Scan { Id = 1, TagCode = "TAG1", StageId = StageId, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = ngAtUtc, RejectionReason = "Lỗi ngoại quan" };
        SetupExistingScans(new List<Scan> { ngScan });
        SetupExistingReworkUnlocks(new List<ReworkUnlock>
        {
            new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlockAtUtc, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" },
        });

        var result = await _sut.CreateAsync(1, "TAG1");

        Assert.Equal(ScanResult.Ok, result.Result);
        // AC5: bản ghi Ng cũ vẫn còn nguyên trong "DB" (không bị Remove/ghi đè) + có thêm 1 bản ghi Ok mới -> tổng 2 bản ghi.
        Assert.Equal(2, _existingScans.Count);
        Assert.Contains(_existingScans, s => s.Result == ScanResult.Ng && s.RejectionReason == "Lỗi ngoại quan");
        Assert.Contains(_existingScans, s => s.Result == ScanResult.Ok);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(1, It.Is<ScanResultDto>(d => d.Result == ScanResult.Ok), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC4: sau khi mở khóa, scan lại (qua Chế độ Scan NG — không đi qua CreateAsync) vẫn không đạt -> lần Ng thứ 2 lại làm tem bị khóa lại khi công nhân cố CreateAsync tiếp.
    [Fact]
    public async Task CreateAsync_SauKhiMoKhoaLaiBiNgLanNua_TuChoiWaitingReworkUnlockLaiTuLuotNgMoi()
    {
        var ng1AtUtc = DateTime.UtcNow.AddMinutes(-20);
        var unlock1AtUtc = DateTime.UtcNow.AddMinutes(-15);
        var ng2AtUtc = DateTime.UtcNow.AddMinutes(-5); // Sau lần mở khóa 1, tem vẫn NG lần nữa (qua Chế độ Scan NG, US-18).
        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "TAG1", StageId = StageId, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = ng1AtUtc },
            new() { Id = 2, TagCode = "TAG1", StageId = StageId, LineId = 1, Result = ScanResult.Ng, ScannedAtUtc = ng2AtUtc },
        });
        SetupExistingReworkUnlocks(new List<ReworkUnlock>
        {
            new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlock1AtUtc, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" },
        });

        var result = await _sut.CreateAsync(1, "TAG1");

        Assert.Equal(ScanResult.WaitingReworkUnlock, result.Result);
        // FR-10: vẫn giữ đủ lịch sử — 2 lượt Ng cũ + 1 lượt WaitingReworkUnlock mới = 3 bản ghi.
        Assert.Equal(3, _existingScans.Count);
    }

    // Tem chưa từng bị Ng tại công đoạn này -> không bị chặn bởi US-19 (hành vi US-08 giữ nguyên).
    [Fact]
    public async Task CreateAsync_TemChuaTungBiNg_KhongBiChanBoiReworkLock()
    {
        var result = await _sut.CreateAsync(1, "TAG-MOI");

        Assert.Equal(ScanResult.Ok, result.Result);
    }
}
