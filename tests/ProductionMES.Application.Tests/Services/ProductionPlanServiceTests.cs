using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.ProductionPlans;
using ProductionMES.Application.Services.ProductionPlans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ProductionPlanService, bám theo AC1-AC3 của US-05 và AC1 của US-06
/// (Documents/BACKLOG-user-story.md).
/// </summary>
public class ProductionPlanServiceTests
{
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ProductionPlanService _sut;

    public ProductionPlanServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _sut = new ProductionPlanService(_unitOfWorkMock.Object);

        _lineRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 1, Name = "Line 1", IsActive = true });
    }

    // AC1 — Tạo kế hoạch mới: kế hoạch được tạo, chưa active, có thể kích hoạt sau đó.
    [Fact]
    public async Task CreateAsync_LineDangHoatDong_TaoKeHoachChuaActive()
    {
        var request = new CreateProductionPlanRequest
        {
            LineId = 1,
            ProductCode = "SP001",
            ProductName = "Sản phẩm A",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30,
            Shift = "Ca 1",
            EffectiveDate = new DateTime(2026, 8, 11),
        };

        var result = await _sut.CreateAsync(request);

        Assert.False(result.IsActive);
        _productionPlanRepositoryMock.Verify(r => r.AddAsync(It.Is<ProductionPlan>(k => !k.IsActive), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_LineKhongHoatDong_NemBusinessRuleException()
    {
        _lineRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Line { Id = 2, Name = "Line 2", IsActive = false });

        var request = new CreateProductionPlanRequest { LineId = 2, ProductCode = "SP", ProductName = "SP", PlannedQuantity = 1, TaktTimeSeconds = 10, Shift = "Ca 1", EffectiveDate = DateTime.Today };

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_LineKhongTonTai_NemEntityNotFoundException()
    {
        var request = new CreateProductionPlanRequest { LineId = 99, ProductCode = "SP", ProductName = "SP", PlannedQuantity = 1, TaktTimeSeconds = 10, Shift = "Ca 1", EffectiveDate = DateTime.Today };

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateAsync(request));
    }

    // AC2 — Một Line chỉ có 1 kế hoạch active tại 1 thời điểm.
    [Fact]
    public async Task ActivateAsync_LineDaCoKeHoachActiveKhac_NemBusinessRuleException()
    {
        var newProductionPlan = new ProductionPlan { Id = 2, LineId = 1, TaktTimeSeconds = 30, IsActive = false };
        var activeProductionPlan = new ProductionPlan { Id = 1, LineId = 1, TaktTimeSeconds = 20, IsActive = true };

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(newProductionPlan);
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan> { activeProductionPlan });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ActivateAsync(2));

        Assert.False(newProductionPlan.IsActive);
    }

    [Fact]
    public async Task ActivateAsync_LineChuaCoKeHoachActiveNao_KichHoatThanhCong()
    {
        var productionPlan = new ProductionPlan { Id = 1, LineId = 1, TaktTimeSeconds = 30, IsActive = false };

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(productionPlan);
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan>());

        var result = await _sut.ActivateAsync(1);

        Assert.True(result.IsActive);
        _productionPlanRepositoryMock.Verify(r => r.Update(productionPlan), Times.Once);
    }

    // AC3 — Cập nhật kế hoạch: thông tin được cập nhật.
    [Fact]
    public async Task UpdateAsync_KeHoachDaTonTai_CapNhatThanhCong()
    {
        var existing = new ProductionPlan { Id = 1, LineId = 1, ProductCode = "SP001", ProductName = "Cũ", PlannedQuantity = 100, TaktTimeSeconds = 20, Shift = "Ca 1", EffectiveDate = DateTime.Today };
        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var request = new UpdateProductionPlanRequest
        {
            ProductCode = "SP002",
            ProductName = "Mới",
            PlannedQuantity = 200,
            TaktTimeSeconds = 25,
            Shift = "Ca 2",
            EffectiveDate = DateTime.Today.AddDays(1),
        };

        var result = await _sut.UpdateAsync(1, request);

        Assert.Equal("SP002", result.ProductCode);
        Assert.Equal(200, result.PlannedQuantity);
        _productionPlanRepositoryMock.Verify(r => r.Update(existing), Times.Once);
    }

    // US-06/AC1 (AC-04 gốc): takt time = 30 giây -> sản lượng chuẩn = 120 sản phẩm/giờ.
    [Fact]
    public async Task CreateAsync_TaktTime30Giay_SanLuongChuanMoiGioLa120()
    {
        var request = new CreateProductionPlanRequest
        {
            LineId = 1,
            ProductCode = "SP001",
            ProductName = "Sản phẩm A",
            PlannedQuantity = 1000,
            TaktTimeSeconds = 30,
            Shift = "Ca 1",
            EffectiveDate = DateTime.Today,
        };

        var result = await _sut.CreateAsync(request);

        Assert.Equal(120m, result.StandardQuantityPerHour);
    }
}
