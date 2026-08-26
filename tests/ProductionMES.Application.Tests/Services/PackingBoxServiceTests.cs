using System.IO;
using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Storage;
using ProductionMES.Application.DTOs.PackingModelConfigs;
using ProductionMES.Application.Services.PackingBoxes;
using ProductionMES.Application.Services.PackingModelConfigs;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho PackingBoxService, bám theo AC2/AC4/AC5/AC6/AC7/AC8/AC11/AC12/AC13 của US-25
/// (Documents/backlog/US-25-quet-tem-dong-thung/README.md). AC1/AC3/AC9/AC10/AC14 kế thừa trực tiếp từ FR-08
/// (ScanService, đã có test riêng) hoặc chỉ là hành vi UI, không lặp lại ở đây.
/// </summary>
public class PackingBoxServiceTests
{
    private const int WorkStationId = 1;
    private const int LineId = 10;
    private const int StageId = 100; // "Đóng thùng"
    private const int ProductionPlanId = 500;

    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<PackingBox>> _packingBoxRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPackingModelConfigService> _packingModelConfigServiceMock = new();
    private readonly Mock<IPackingTemplateStorage> _templateStorageMock = new();
    private readonly Mock<IPackingLabelGenerator> _labelGeneratorMock = new();
    private readonly PackingBoxService _sut;

    private List<PackingBox> _existingBoxes = new();

    public PackingBoxServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<PackingBox>()).Returns(_packingBoxRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);

        _sut = new PackingBoxService(_unitOfWorkMock.Object, _packingModelConfigServiceMock.Object, _templateStorageMock.Object, _labelGeneratorMock.Object);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(WorkStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = WorkStationId, LineId = LineId, StageId = StageId, Name = "Trạm đóng thùng" });

        var runningPlanStage = new ProductionPlanStage { Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = StageId, PlanStatus = PlanStatus.Running };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { runningPlanStage });

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(ProductionPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = ProductionPlanId, LineId = LineId, Model = "ABC-123", PlannedQuantity = 1000 });

        SetupExistingBoxes(new List<PackingBox>());
    }

    private void SetupExistingBoxes(List<PackingBox> boxes)
    {
        _existingBoxes = boxes;
        _packingBoxRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PackingBox, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PackingBox, bool>> predicate, CancellationToken _) => _existingBoxes.Where(predicate.Compile()).ToList());
        _packingBoxRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()))
            .Callback<PackingBox, CancellationToken>((box, _) => _existingBoxes.Add(box))
            .Returns(Task.CompletedTask);
    }

    private static PackingModelConfigDto SampleConfig(int id = 9, int packingQuantity = 5) => new()
    {
        Id = id,
        Model = "ABC-123",
        PackingQuantity = packingQuantity,
        GrossWeight = 12.5m,
        PartName = "Sản phẩm A",
        Manufacturer = "Nhà máy X",
        HasTemplate = true,
    };

    // AC5 — Chưa từng có thùng nào -> GetStateAsync trả RequiresStartingBoxNo = true.
    [Fact]
    public async Task GetStateAsync_ChuaCoThungNao_TraVeRequiresStartingBoxNoTrue()
    {
        var result = await _sut.GetStateAsync(WorkStationId);

        Assert.True(result.RequiresStartingBoxNo);
        Assert.Null(result.CurrentBox);
    }

    // AC6 — Đã có thùng đang dở -> GetStateAsync trả đúng thùng hiện tại, không yêu cầu nhập lại BoxNo.
    [Fact]
    public async Task GetStateAsync_DaCoThungDangDo_TraVeDungThungHienTai()
    {
        SetupExistingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = ProductionPlanId, StageId = StageId, BoxNo = 5, Status = PackingBoxStatus.InProgress, TargetQuantity = 10, ScannedQuantity = 3, ModelSnapshot = "ABC-123", PartNameSnapshot = "Sản phẩm A" },
        });

        var result = await _sut.GetStateAsync(WorkStationId);

        Assert.False(result.RequiresStartingBoxNo);
        Assert.NotNull(result.CurrentBox);
        Assert.Equal(5, result.CurrentBox!.BoxNo);
        Assert.Equal(3, result.CurrentBox.ScannedQuantity);
    }

    // AC5 — Nhập số thùng bắt đầu thành công, snapshot đúng Quy cách đóng gói hiện tại (AC12).
    [Fact]
    public async Task SetStartingBoxNoAsync_ChuaCoThungNao_TaoMoiThanhCongVaSnapshotDungQuyCach()
    {
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleConfig());

        var result = await _sut.SetStartingBoxNoAsync(WorkStationId, 7);

        Assert.Equal(7, result.BoxNo);
        Assert.Equal(PackingBoxStatus.InProgress, result.Status);
        Assert.Equal(5, result.TargetQuantity);
        Assert.Equal(0, result.ScannedQuantity);
        Assert.Equal("Sản phẩm A", result.PartName);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC5 — Kế hoạch đã có thùng (bất kể trạng thái) -> từ chối nhập lại số thùng bắt đầu.
    [Fact]
    public async Task SetStartingBoxNoAsync_KeHoachDaCoThung_NemBusinessRuleException()
    {
        SetupExistingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = ProductionPlanId, StageId = StageId, BoxNo = 1, Status = PackingBoxStatus.Completed, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" },
        });
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleConfig());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetStartingBoxNoAsync(WorkStationId, 2));
    }

    // AC11 — Model chưa có cấu hình đóng gói -> chặn nhập số thùng bắt đầu.
    [Fact]
    public async Task SetStartingBoxNoAsync_ModelChuaCoCauHinhDongGoi_NemBusinessRuleException()
    {
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackingModelConfigDto?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetStartingBoxNoAsync(WorkStationId, 1));
        _packingBoxRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetStartingBoxNoAsync_SoThungKhongHopLe_NemBusinessRuleException()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetStartingBoxNoAsync(WorkStationId, 0));
    }

    // AC11 — EnsureReadyForScanAsync chặn khi Model chưa có cấu hình đóng gói (KHÔNG lưu bản ghi Scan nào — chỉ đảm bảo không tạo box/không throw sai loại).
    [Fact]
    public async Task EnsureReadyForScanAsync_ModelChuaCoCauHinhDongGoi_NemBusinessRuleException()
    {
        var workStation = new WorkStation { Id = WorkStationId, LineId = LineId, StageId = StageId };
        var plan = new ProductionPlan { Id = ProductionPlanId, Model = "ABC-123" };
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PackingModelConfigDto?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EnsureReadyForScanAsync(workStation, plan));
    }

    // AC5 — EnsureReadyForScanAsync chặn khi chưa nhập số thùng bắt đầu (dù đã có cấu hình đóng gói).
    [Fact]
    public async Task EnsureReadyForScanAsync_ChuaNhapSoThungBatDau_NemBusinessRuleException()
    {
        var workStation = new WorkStation { Id = WorkStationId, LineId = LineId, StageId = StageId };
        var plan = new ProductionPlan { Id = ProductionPlanId, Model = "ABC-123" };
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleConfig());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.EnsureReadyForScanAsync(workStation, plan));
    }

    // AC2 — Đã có thùng đang mở -> EnsureReadyForScanAsync trả về đúng thùng đó (sẵn sàng cho ScanService cộng dồn).
    [Fact]
    public async Task EnsureReadyForScanAsync_DaCoThungDangMo_TraVeDungThungHienTai()
    {
        var box = new PackingBox { Id = 1, ProductionPlanId = ProductionPlanId, StageId = StageId, BoxNo = 3, Status = PackingBoxStatus.InProgress, TargetQuantity = 5, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        SetupExistingBoxes(new List<PackingBox> { box });
        var workStation = new WorkStation { Id = WorkStationId, LineId = LineId, StageId = StageId };
        var plan = new ProductionPlan { Id = ProductionPlanId, Model = "ABC-123" };
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(SampleConfig());

        var result = await _sut.EnsureReadyForScanAsync(workStation, plan);

        Assert.Equal(box.Id, result.Id);
    }

    // AC2 — Chưa đủ số lượng -> chỉ tăng đếm, thùng vẫn InProgress, KHÔNG tạo thùng mới.
    [Fact]
    public async Task RegisterOkScanAsync_ChuaDuSoLuong_ChiTangDemKhongHoanTat()
    {
        var box = new PackingBox { Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = StageId, WorkStationId = WorkStationId, BoxNo = 1, Status = PackingBoxStatus.InProgress, TargetQuantity = 5, ScannedQuantity = 2, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };

        var outcome = await _sut.RegisterOkScanAsync(box);

        Assert.False(outcome.BoxCompleted);
        Assert.Equal(3, outcome.ScannedQuantity);
        Assert.Equal(1, outcome.BoxNo);
        Assert.Equal(PackingBoxStatus.InProgress, box.Status);
        _packingBoxRepositoryMock.Verify(r => r.AddAsync(It.IsAny<PackingBox>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC4/AC12 — Vừa đủ số lượng -> hoàn tất thùng hiện tại VÀ tự động mở thùng kế tiếp, snapshot theo Quy cách đóng gói MỚI NHẤT (không phải snapshot cũ).
    [Fact]
    public async Task RegisterOkScanAsync_VuaDuSoLuong_HoanTatVaMoThungKeTiepSnapshotQuyCachMoiNhat()
    {
        var box = new PackingBox
        {
            Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = StageId, WorkStationId = WorkStationId,
            BoxNo = 1, Status = PackingBoxStatus.InProgress, TargetQuantity = 5, ScannedQuantity = 4,
            PackingModelConfigId = 9, ModelSnapshot = "ABC-123", PartNameSnapshot = "Sản phẩm CŨ", GrossWeightSnapshot = 1m,
        };

        // AC12: Quy cách đóng gói ĐÃ ĐƯỢC SỬA (quantity/partName mới) kể từ lúc mở thùng #1 — thùng kế tiếp phải dùng giá trị MỚI.
        var updatedConfig = SampleConfig(id: 9, packingQuantity: 8);
        updatedConfig.PartName = "Sản phẩm MỚI";
        _packingModelConfigServiceMock.Setup(s => s.GetByModelAsync("ABC-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedConfig);

        var outcome = await _sut.RegisterOkScanAsync(box);

        Assert.True(outcome.BoxCompleted);
        Assert.Equal(1, outcome.CompletedBoxId);
        Assert.Equal(PackingBoxStatus.Completed, box.Status);
        Assert.NotNull(box.CompletedAtUtc);

        Assert.Equal(2, outcome.BoxNo); // AC4: tự động tăng
        Assert.Equal(0, outcome.ScannedQuantity); // AC4: reset về 0
        Assert.Equal(8, outcome.TargetQuantity); // AC12: theo quy cách MỚI, không phải snapshot cũ (5)

        _packingBoxRepositoryMock.Verify(r => r.AddAsync(
            It.Is<PackingBox>(b => b.BoxNo == 2 && b.PartNameSnapshot == "Sản phẩm MỚI" && b.TargetQuantity == 8 && b.Status == PackingBoxStatus.InProgress),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC7 — Sửa số thùng hiện tại thành công, không đổi số lượng đã quét/mục tiêu.
    [Fact]
    public async Task UpdateCurrentBoxNoAsync_DangCoThungMo_SuaThanhCong()
    {
        var box = new PackingBox { Id = 1, ProductionPlanId = ProductionPlanId, StageId = StageId, BoxNo = 3, Status = PackingBoxStatus.InProgress, TargetQuantity = 5, ScannedQuantity = 2, ModelSnapshot = "ABC-123", PartNameSnapshot = "A" };
        SetupExistingBoxes(new List<PackingBox> { box });

        var result = await _sut.UpdateCurrentBoxNoAsync(WorkStationId, 99, updatedByUserId: 7, updatedByUserName: "supervisor01");

        Assert.Equal(99, result.BoxNo);
        Assert.Equal(2, result.ScannedQuantity);
        Assert.Equal(5, result.TargetQuantity);
    }

    [Fact]
    public async Task UpdateCurrentBoxNoAsync_ChuaCoThungDangMo_NemBusinessRuleException()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateCurrentBoxNoAsync(WorkStationId, 5, 7, "supervisor01"));
    }

    [Fact]
    public async Task UpdateCurrentBoxNoAsync_ThieuThongTinNguoiSua_NemBusinessRuleException()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateCurrentBoxNoAsync(WorkStationId, 5, 0, string.Empty));
    }

    // US-27 (25/08/2026): ConfirmDuplicateAsync (US-25 AC8) đã bị xóa khỏi PackingBoxService — cơ chế audit riêng
    // này SUPERSEDED bởi Scan.ConfirmReject đồng nhất (US-27 AC12), test tương ứng chuyển sang ScanService.

    // AC13 — Model chưa có mẫu tem -> chặn tạo tem (lỗi CHÍNH lệnh gọi in, không phải lỗi vật lý).
    [Fact]
    public async Task GenerateLabelAsync_ChuaCoMauTem_NemBusinessRuleException()
    {
        var box = new PackingBox
        {
            Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = StageId, WorkStationId = WorkStationId,
            BoxNo = 1, PackingModelConfigId = 9, ModelSnapshot = "ABC-123", PartNameSnapshot = "A",
        };
        _packingBoxRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(box);
        _templateStorageMock.Setup(s => s.OpenReadAsync(9, It.IsAny<CancellationToken>())).ReturnsAsync((Stream?)null);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GenerateLabelAsync(1));
    }

    // AC4 — Có mẫu tem -> merge dữ liệu qua IPackingLabelGenerator, trả về đúng file.
    [Fact]
    public async Task GenerateLabelAsync_CoMauTem_TraVeFileDaMergeDungDuLieu()
    {
        var box = new PackingBox
        {
            Id = 1, ProductionPlanId = ProductionPlanId, LineId = LineId, StageId = StageId, WorkStationId = WorkStationId,
            BoxNo = 7, PackingModelConfigId = 9, ModelSnapshot = "ABC-123", PartNameSnapshot = "Sản phẩm A", TargetQuantity = 5,
        };
        _packingBoxRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(box);
        _templateStorageMock.Setup(s => s.OpenReadAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(LineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = LineId, Name = "Line 1" });
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(WorkStationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = WorkStationId, Name = "Trạm đóng thùng" });

        var expectedBytes = new byte[] { 9, 9, 9 };
        _labelGeneratorMock
            .Setup(g => g.Generate(It.IsAny<byte[]>(), It.Is<PackingLabelData>(d => d.BoxNo == 7 && d.Model == "ABC-123" && d.LineName == "Line 1" && d.WorkStationName == "Trạm đóng thùng")))
            .Returns(expectedBytes);

        var (content, fileName) = await _sut.GenerateLabelAsync(1);

        Assert.Equal(expectedBytes, content);
        Assert.Contains("box7", fileName);
    }
}
