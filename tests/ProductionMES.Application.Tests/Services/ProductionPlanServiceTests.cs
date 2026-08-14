using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.ProductionPlans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ProductionPlanService, bám theo AC1/AC2/AC4/AC5 của US-05 và AC1 của US-06
/// (Documents/BACKLOG-user-story.md, cập nhật 13/08/2026). AC1 (US-05a — ràng buộc active theo cặp Line/Công
/// đoạn) và AC5 (US-05a — đóng sớm) được kiểm thử ở ProductionPlanStageServiceTests, vì vòng đời trạng thái nay
/// thuộc entity ProductionPlanStage.
/// </summary>
public class ProductionPlanServiceTests
{
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProductionPlanService _sut;

    public ProductionPlanServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _sut = new ProductionPlanService(_unitOfWorkMock.Object);

        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 1, Name = "Line 1", IsActive = true });

        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());

        // AC6 (US-05): mặc định kế hoạch CHƯA có bản ghi Scan nào — từng test override qua SetupExistingScans khi cần.
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Scan>());
    }

    /// <summary>Mô phỏng kế hoạch ĐÃ có bản ghi Scan (AC6 — khóa sửa Customer/Model/Lot/Revision).</summary>
    private void SetupExistingScans(List<Scan> existingScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                existingScans.Where(predicate.Compile()).ToList());
    }

    private static CreateProductionPlanRequest BuildValidCreateRequest(int lineId = 1) => new()
    {
        LineId = lineId,
        Customer = "Khách hàng A",
        Model = "Model X",
        Lot = "LOT001",
        Revision = "A",
        PlannedQuantity = 1000,
        TaktTimeSeconds = 30,
        StartTime = new DateTime(2026, 8, 11, 7, 30, 0),
        OperatorNames = "Nguyễn Văn A, Trần Thị B",
    };

    // AC1 — Tạo kế hoạch mới: tự nhiên ở trạng thái Draft (chưa có ProductionPlanStage nào).
    [Fact]
    public async Task CreateAsync_LineDangHoatDong_TaoKeHoachThanhCong()
    {
        var request = BuildValidCreateRequest();

        var result = await _sut.CreateAsync(request);

        Assert.Equal("Model X", result.Model);
        Assert.Equal("LOT001", result.Lot);
        _productionPlanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ProductionPlan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC2 — Revision để trống vẫn cho lưu bình thường.
    [Fact]
    public async Task CreateAsync_RevisionDeTrong_VanTaoThanhCong()
    {
        var request = BuildValidCreateRequest();
        request.Revision = null;

        var result = await _sut.CreateAsync(request);

        Assert.Null(result.Revision);
    }

    [Fact]
    public async Task CreateAsync_LineKhongHoatDong_NemBusinessRuleException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 2, Name = "Line 2", IsActive = false });

        var request = BuildValidCreateRequest(2);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        var request = BuildValidCreateRequest(99);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateAsync(request));
    }

    // AC4 — Cập nhật kế hoạch chưa từng Running (Draft): tự do cập nhật, không cần Confirm.
    [Fact]
    public async Task UpdateAsync_KeHoachDraftChuaCoCongDoanNaoRunning_CapNhatTuDoKhongCanConfirm()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "Cũ", Model = "Model cũ", Lot = "LOT-CU",
            PlannedQuantity = 100, TaktTimeSeconds = 20, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateProductionPlanRequest
        {
            Customer = "Mới",
            Model = "Model mới",
            Lot = "LOT-MOI",
            PlannedQuantity = 200,
            TaktTimeSeconds = 25,
            StartTime = DateTime.Today.AddDays(1),
            OperatorNames = "B",
            Confirm = false,
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal("Model mới", result.Model);
        Assert.Equal(200, result.PlannedQuantity);
        _productionPlanRepositoryMock.Verify(r => r.Update(existing), Times.Once);
    }

    // AC5 — Sửa Số lượng/Takt time khi đã có công đoạn Running/Paused mà chưa Confirm -> từ chối, cảnh báo rõ.
    [Fact]
    public async Task UpdateAsync_DoiSoLuongKhiCoCongDoanRunning_ChuaConfirm_NemBusinessRuleException()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "A", Model = "M", Lot = "L",
            PlannedQuantity = 1000, TaktTimeSeconds = 30, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { new() { Id = 1, ProductionPlanId = 1, StageId = 10, PlanStatus = PlanStatus.Running } });

        var request = new UpdateProductionPlanRequest
        {
            Customer = "A", Model = "M", Lot = "L",
            PlannedQuantity = 500, // đổi số lượng
            TaktTimeSeconds = 30,
            StartTime = DateTime.Today,
            OperatorNames = "A",
            Confirm = false,
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, request));
        _productionPlanRepositoryMock.Verify(r => r.Update(It.IsAny<ProductionPlan>()), Times.Never);
    }

    // AC5 — Cùng tình huống trên nhưng đã Confirm = true -> cho phép cập nhật.
    [Fact]
    public async Task UpdateAsync_DoiSoLuongKhiCoCongDoanRunning_DaConfirm_CapNhatThanhCong()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "A", Model = "M", Lot = "L",
            PlannedQuantity = 1000, TaktTimeSeconds = 30, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { new() { Id = 1, ProductionPlanId = 1, StageId = 10, PlanStatus = PlanStatus.Running } });

        var request = new UpdateProductionPlanRequest
        {
            Customer = "A", Model = "M", Lot = "L",
            PlannedQuantity = 500,
            TaktTimeSeconds = 30,
            StartTime = DateTime.Today,
            OperatorNames = "A",
            Confirm = true,
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal(500, result.PlannedQuantity);
        _productionPlanRepositoryMock.Verify(r => r.Update(existing), Times.Once);
    }

    // AC5 (mặt trái): sửa các trường KHÔNG phải Số lượng/Takt time, dù có công đoạn Running -> không cần Confirm.
    [Fact]
    public async Task UpdateAsync_SuaTruongKhacKhongPhaiSoLuongHayTaktTime_KhongCanConfirmDuCoCongDoanRunning()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "A", Model = "M", Lot = "L",
            PlannedQuantity = 1000, TaktTimeSeconds = 30, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage> { new() { Id = 1, ProductionPlanId = 1, StageId = 10, PlanStatus = PlanStatus.Running } });

        var request = new UpdateProductionPlanRequest
        {
            Customer = "Khách hàng mới", // chỉ đổi Customer
            Model = "M",
            Lot = "L",
            PlannedQuantity = 1000, // giữ nguyên
            TaktTimeSeconds = 30, // giữ nguyên
            StartTime = DateTime.Today,
            OperatorNames = "A",
            Confirm = false,
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal("Khách hàng mới", result.Customer);
    }

    // AC6 — Sửa Model của kế hoạch ĐÃ có ít nhất 1 bản ghi Scan -> ném BusinessRuleException, dù có Confirm=true hay không.
    [Fact]
    public async Task UpdateAsync_SuaModelKeHoachDaCoScan_NemBusinessRuleException()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "A", Model = "Model cũ", Lot = "L",
            PlannedQuantity = 1000, TaktTimeSeconds = 30, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        SetupExistingScans(new List<Scan> { new() { Id = 1, ProductionPlanId = 1, TagCode = "T1", Result = ScanResult.Ok } });

        var request = new UpdateProductionPlanRequest
        {
            Customer = "A",
            Model = "Model mới", // đổi Model
            Lot = "L",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30,
            StartTime = DateTime.Today,
            OperatorNames = "A",
            Confirm = true, // Confirm=true KHÔNG override được rule này
        };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UpdateAsync(1, request));
        _productionPlanRepositoryMock.Verify(r => r.Update(It.IsAny<ProductionPlan>()), Times.Never);
    }

    // AC6 (mặt trái) — Sửa Model của kế hoạch CHƯA có bản ghi Scan nào -> thành công bình thường.
    [Fact]
    public async Task UpdateAsync_SuaModelKeHoachChuaCoScan_CapNhatThanhCong()
    {
        var existing = new ProductionPlan
        {
            Id = 1, LineId = 1, Customer = "A", Model = "Model cũ", Lot = "L",
            PlannedQuantity = 1000, TaktTimeSeconds = 30, StartTime = DateTime.Today, OperatorNames = "A",
        };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        // Mặc định constructor: không có Scan nào.

        var request = new UpdateProductionPlanRequest
        {
            Customer = "A",
            Model = "Model mới",
            Lot = "L",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30,
            StartTime = DateTime.Today,
            OperatorNames = "A",
            Confirm = false,
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal("Model mới", result.Model);
        _productionPlanRepositoryMock.Verify(r => r.Update(existing), Times.Once);
    }

    // US-06/AC1 (AC-04 gốc): takt time = 30 giây -> sản lượng chuẩn = 120 sản phẩm/giờ.
    [Fact]
    public async Task CreateAsync_TaktTime30Giay_SanLuongChuanMoiGioLa120()
    {
        var request = BuildValidCreateRequest();
        request.TaktTimeSeconds = 30;

        var result = await _sut.CreateAsync(request);

        Assert.Equal(120m, result.StandardQuantityPerHour);
    }
}
