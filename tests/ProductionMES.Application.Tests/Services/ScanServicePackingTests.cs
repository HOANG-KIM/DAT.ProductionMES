using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.PackingBoxes;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho tích hợp US-25 (công đoạn "Đóng thùng") vào <see cref="ScanService.CreateAsync"/> — AC1 (không có
/// luồng/API riêng), AC2 (cộng dồn khi Ok), AC5/AC11 (chặn qua <see cref="IPackingBoxService.EnsureReadyForScanAsync"/>,
/// KHÔNG lưu Scan), AC14 (Stage khác giữ nguyên hành vi, không gọi <see cref="IPackingBoxService"/>). Logic đếm/
/// snapshot/audit chi tiết đã có <c>PackingBoxServiceTests</c> riêng, không lặp lại ở đây.
/// </summary>
public class ScanServicePackingTests
{
    private const int WorkStationId = 1;
    private const int LineId = 10;
    private const int PackingStageId = 300; // "Đóng thùng"
    private const int ProductionPlanId = 700;

    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<ReworkUnlock>> _reworkUnlockRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductionPlanStageService> _productionPlanStageServiceMock = new();
    private readonly Mock<IScanNotifier> _scanNotifierMock = new();
    private readonly Mock<IPackingBoxService> _packingBoxServiceMock = new();
    private readonly ScanService _sut;

    private readonly List<Scan> _existingScans = new();

    public ScanServicePackingTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object, _packingBoxServiceMock.Object);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(WorkStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = WorkStationId, LineId = LineId, StageId = PackingStageId, Name = "Trạm Đóng thùng" });

        var runningPlanStage = new ProductionPlanStage { Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = PackingStageId, PlanStatus = PlanStatus.Running };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { runningPlanStage });

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(ProductionPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = ProductionPlanId, LineId = LineId, Model = "ABC-123", PlannedQuantity = 1_000_000 });

        // Công đoạn "Đóng thùng" là công đoạn đầu tiên trong trình tự test này (không có liền trước) — tập trung
        // vào tích hợp US-25, không lặp lại test FR-08 "công đoạn liền trước" đã có ở ScanServiceTests.
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(ProductionPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = ProductionPlanId, StageId = PackingStageId, SequenceNumber = 1, PreviousStageId = null },
            });

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
    }

    private void SetupPackingStage(bool isPackingStage) =>
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(PackingStageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = PackingStageId, Name = "Đóng thùng", IsActive = true, IsPackingStage = isPackingStage });

    // AC1/AC2 — Đóng thùng là 1 Stage bình thường, tuân thủ đúng FR-08 (không có luồng riêng) — scan hợp lệ vẫn
    // đi qua CreateAsync như mọi Stage khác, CHỈ khác ở chỗ có thêm bước cộng dồn thùng khi Ok.
    [Fact]
    public async Task CreateAsync_TaiCongDoanDongThung_ScanHopLe_GoiEnsureReadyVaRegisterOkScanCongDonThung()
    {
        SetupPackingStage(true);
        var box = new PackingBox { Id = 5, ProductionPlanId = ProductionPlanId, StageId = PackingStageId, BoxNo = 2, TargetQuantity = 10, ScannedQuantity = 3, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        _packingBoxServiceMock.Setup(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);
        _packingBoxServiceMock.Setup(s => s.RegisterOkScanAsync(box, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackingScanOutcome { BoxId = 5, BoxNo = 2, ScannedQuantity = 4, TargetQuantity = 10, BoxCompleted = false });

        var result = await _sut.CreateAsync(WorkStationId, "TAG001");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.True(result.IsPackingStage);
        Assert.Equal(2, result.PackingBoxNo);
        Assert.Equal(4, result.PackingScannedQuantity);
        Assert.Equal(10, result.PackingTargetQuantity);
        Assert.False(result.PackingBoxCompleted);
        Assert.Null(result.PackingCompletedBoxId);
        _packingBoxServiceMock.Verify(s => s.RegisterOkScanAsync(box, It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC4 — Vừa đủ số lượng -> ScanResultDto phản ánh đúng PackingBoxCompleted + Id thùng vừa hoàn tất, đồng thời
    // các field còn lại (BoxNo/ScannedQuantity/TargetQuantity) là của thùng KẾ TIẾP (đã tự động mở, AC4).
    [Fact]
    public async Task CreateAsync_TaiCongDoanDongThung_VuaDuSoLuong_PhanAnhDungBoxCompletedVaThungKeTiep()
    {
        SetupPackingStage(true);
        var box = new PackingBox { Id = 5, ProductionPlanId = ProductionPlanId, StageId = PackingStageId, BoxNo = 2, TargetQuantity = 10, ScannedQuantity = 9, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        _packingBoxServiceMock.Setup(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);
        _packingBoxServiceMock.Setup(s => s.RegisterOkScanAsync(box, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackingScanOutcome { BoxId = 6, BoxNo = 3, ScannedQuantity = 0, TargetQuantity = 10, BoxCompleted = true, CompletedBoxId = 5 });

        var result = await _sut.CreateAsync(WorkStationId, "TAG002");

        Assert.True(result.PackingBoxCompleted);
        Assert.Equal(5, result.PackingCompletedBoxId);
        Assert.Equal(3, result.PackingBoxNo);
        Assert.Equal(0, result.PackingScannedQuantity);
    }

    // AC11/AC5 — EnsureReadyForScanAsync từ chối (Model chưa cấu hình đóng gói, hoặc chưa nhập số thùng bắt đầu)
    // -> ném BusinessRuleException, KHÔNG lưu bản ghi Scan nào (khác DuplicateTag/PreviousStageNotPassed).
    [Fact]
    public async Task CreateAsync_TaiCongDoanDongThung_ChuaDuDieuKienDongThung_NemBusinessRuleExceptionKhongLuuScan()
    {
        SetupPackingStage(true);
        _packingBoxServiceMock.Setup(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BusinessRuleException("Chưa nhập số thùng bắt đầu."));

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(WorkStationId, "TAG003"));

        Assert.Empty(_existingScans);
        _packingBoxServiceMock.Verify(s => s.RegisterOkScanAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC14 — Stage KHÔNG phải "Đóng thùng" -> KHÔNG gọi IPackingBoxService, ScanResultDto.IsPackingStage = false, hành vi y hệt US-07/US-08.
    [Fact]
    public async Task CreateAsync_TaiCongDoanKhacDongThung_KhongGoiPackingBoxService()
    {
        SetupPackingStage(false);

        var result = await _sut.CreateAsync(WorkStationId, "TAG004");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.False(result.IsPackingStage);
        Assert.Null(result.PackingBoxNo);
        _packingBoxServiceMock.Verify(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()), Times.Never);
        _packingBoxServiceMock.Verify(s => s.RegisterOkScanAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC8 — Tem trùng TẠI "Đóng thùng" vẫn bị từ chối đúng theo FR-08 (không ghi đè) — RegisterOkScanAsync KHÔNG được gọi cho lượt bị từ chối này.
    [Fact]
    public async Task CreateAsync_TaiCongDoanDongThung_TrungTem_TuChoiDuplicateTagKhongCongDonThung()
    {
        SetupPackingStage(true);
        var box = new PackingBox { Id = 5, ProductionPlanId = ProductionPlanId, StageId = PackingStageId, BoxNo = 2, TargetQuantity = 10, ScannedQuantity = 3, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        _packingBoxServiceMock.Setup(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);
        _existingScans.Add(new Scan { TagCode = "TAG005", StageId = PackingStageId, Result = ScanResult.Ok });

        var result = await _sut.CreateAsync(WorkStationId, "TAG005");

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.True(result.IsPackingStage);
        _packingBoxServiceMock.Verify(s => s.RegisterOkScanAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-26 AC7/AC8 — Scan Ok tại công đoạn Đóng thùng phải gắn đúng Scan.PackingBoxId = Id của currentPackingBox
    // (đã xác định TRƯỚC khi lưu Scan, qua EnsureReadyForScanAsync) — phục vụ xem chi tiết lượt scan theo thùng (AC7).
    [Fact]
    public async Task CreateAsync_TaiCongDoanDongThung_ScanOk_GanDungPackingBoxId()
    {
        SetupPackingStage(true);
        var box = new PackingBox { Id = 5, ProductionPlanId = ProductionPlanId, StageId = PackingStageId, BoxNo = 2, TargetQuantity = 10, ScannedQuantity = 3, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        _packingBoxServiceMock.Setup(s => s.EnsureReadyForScanAsync(It.IsAny<WorkStation>(), It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);
        _packingBoxServiceMock.Setup(s => s.RegisterOkScanAsync(box, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PackingScanOutcome { BoxId = 5, BoxNo = 2, ScannedQuantity = 4, TargetQuantity = 10, BoxCompleted = false });

        await _sut.CreateAsync(WorkStationId, "TAG006");

        var savedScan = Assert.Single(_existingScans);
        Assert.Equal(ScanResult.Ok, savedScan.Result);
        Assert.Equal(5, savedScan.PackingBoxId);
    }

    // AC8 — Stage KHÔNG phải "Đóng thùng" -> PackingBoxId luôn null (currentPackingBox không được xác định).
    [Fact]
    public async Task CreateAsync_TaiCongDoanKhacDongThung_ScanOk_PackingBoxIdLuonNull()
    {
        SetupPackingStage(false);

        await _sut.CreateAsync(WorkStationId, "TAG007");

        var savedScan = Assert.Single(_existingScans);
        Assert.Equal(ScanResult.Ok, savedScan.Result);
        Assert.Null(savedScan.PackingBoxId);
    }
}
