using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Lots;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="LotService"/> (US-21a, viết lại hoàn toàn 19/08/2026; US-05 AC7-AC9).
/// </summary>
public class LotServiceTests
{
    private readonly Mock<IRepository<Lot>> _lotRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<LotHistory>> _lotHistoryRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILotReportService> _lotReportServiceMock = new();
    private readonly LotService _sut;

    public LotServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Lot>()).Returns(_lotRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<LotHistory>()).Returns(_lotHistoryRepositoryMock.Object);
        _sut = new LotService(_unitOfWorkMock.Object, _lotReportServiceMock.Object);

        SetupLots(new List<Lot>());
        SetupPlans(new List<ProductionPlan>());
        // Mặc định: Lot chưa có lịch sử sản xuất nào (LotReportService trả về null) -> không có vi phạm soft-confirm.
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync(It.IsAny<string>(), null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((LotSummaryDto?)null);
    }

    private void SetupLots(List<Lot> lots) =>
        _lotRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Lot, bool>> predicate, CancellationToken _) => lots.Where(predicate.Compile()).ToList());

    private void SetupPlans(List<ProductionPlan> plans) =>
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlan, bool>> predicate, CancellationToken _) => plans.Where(predicate.Compile()).ToList());

    // Lấy Lot theo Code, chưa từng có ai nhập -> null ("Chưa xác định").
    [Fact]
    public async Task GetByCodeAsync_ChuaTonTai_TraVeNull()
    {
        var result = await _sut.GetByCodeAsync("LOT-A");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCodeAsync_DaTonTai_TraVeDungGiaTri()
    {
        SetupLots(new List<Lot> { new() { Id = 1, Code = "LOT-A", TotalQuantity = 1000 } });

        var result = await _sut.GetByCodeAsync("LOT-A");

        Assert.NotNull(result);
        Assert.Equal(1000, result!.TotalQuantity);
    }

    [Fact]
    public async Task GetByCodesAsync_TraNhieuLotCungLuc_TraVeDungDictionary()
    {
        SetupLots(new List<Lot>
        {
            new() { Id = 1, Code = "LOT-A", TotalQuantity = 1000 },
            new() { Id = 2, Code = "LOT-B", TotalQuantity = 500 },
            new() { Id = 3, Code = "LOT-C", TotalQuantity = 200 }, // không nằm trong danh sách truy vấn
        });

        var result = await _sut.GetByCodesAsync(new[] { "LOT-A", "LOT-B" });

        Assert.Equal(2, result.Count);
        Assert.Equal(1000, result["LOT-A"].TotalQuantity);
        Assert.Equal(500, result["LOT-B"].TotalQuantity);
    }

    [Fact]
    public async Task GetByCodesAsync_DanhSachRong_TraVeDictionaryRong()
    {
        var result = await _sut.GetByCodesAsync(Array.Empty<string>());

        Assert.Empty(result);
    }

    // US-05 AC7: Lot chưa từng có ProductionPlan nào -> HasAnyProductionPlanAsync = false.
    [Fact]
    public async Task HasAnyProductionPlanAsync_ChuaTungCoKeHoach_TraVeFalse()
    {
        var result = await _sut.HasAnyProductionPlanAsync("LOT-MOI");

        Assert.False(result);
    }

    [Fact]
    public async Task HasAnyProductionPlanAsync_DaTungCoKeHoach_TraVeTrue()
    {
        SetupPlans(new List<ProductionPlan> { new() { Id = 1, Lot = "LOT-A" } });

        var result = await _sut.HasAnyProductionPlanAsync("LOT-A");

        Assert.True(result);
    }

    // US-21a AC1: chưa có row Lot -> upsert tạo mới.
    [Fact]
    public async Task UpsertTotalQuantityAsync_ChuaCoRowLot_TaoMoi()
    {
        var result = await _sut.UpsertTotalQuantityAsync("LOT-A", 1000, confirm: false, updatedByUserName: "to.truong");

        Assert.Equal("LOT-A", result.Code);
        Assert.Equal(1000, result.TotalQuantity);
        Assert.Equal("to.truong", result.UpdatedByUserName);
        _lotRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Lot>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Lần đầu đặt giá trị cũng ghi lịch sử, OldTotalQuantity = null (chưa từng có row Lot trước đó).
        _lotHistoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<LotHistory>(h => h.LotCode == "LOT-A" && h.OldTotalQuantity == null && h.NewTotalQuantity == 1000),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // US-21a AC2: đã có row Lot -> upsert cập nhật tại chỗ, không tạo bản ghi mới.
    [Fact]
    public async Task UpsertTotalQuantityAsync_DaCoRowLot_CapNhatTaiCho()
    {
        var existing = new Lot { Id = 1, Code = "LOT-A", TotalQuantity = 1000 };
        SetupLots(new List<Lot> { existing });

        var result = await _sut.UpsertTotalQuantityAsync("LOT-A", 1500, confirm: false, updatedByUserName: "to.truong");

        Assert.Equal(1500, result.TotalQuantity);
        Assert.Equal(1500, existing.TotalQuantity);
        _lotRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Lot>(), It.IsAny<CancellationToken>()), Times.Never);
        _lotRepositoryMock.Verify(r => r.Update(existing), Times.Once);
        _lotHistoryRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<LotHistory>(h => h.LotCode == "LOT-A" && h.OldTotalQuantity == 1000 && h.NewTotalQuantity == 1500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Lưu lại ĐÚNG giá trị cũ (không đổi) -> KHÔNG ghi thêm dòng lịch sử (tránh nhiễu log).
    [Fact]
    public async Task UpsertTotalQuantityAsync_LuuLaiGiaTriCu_KhongGhiLichSu()
    {
        var existing = new Lot { Id = 1, Code = "LOT-A", TotalQuantity = 1000 };
        SetupLots(new List<Lot> { existing });

        await _sut.UpsertTotalQuantityAsync("LOT-A", 1000, confirm: false, updatedByUserName: "to.truong");

        _lotHistoryRepositoryMock.Verify(r => r.AddAsync(It.IsAny<LotHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-21a AC3 (=US-05 AC8): giảm xuống dưới OkCount đã chạy thực tế, chưa Confirm -> từ chối, nêu rõ Line/Công đoạn.
    [Fact]
    public async Task UpsertTotalQuantityAsync_GiamDuoiThucTeChuaConfirm_NemBusinessRuleException()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LotSummaryDto
            {
                Lot = "LOT-A",
                Rows = new List<LotStageRowDto>
                {
                    new() { LineId = 1, LineName = "Line 1", StageId = 10, StageName = "Lắp ráp", OkCount = 800 },
                },
            });

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(
            () => _sut.UpsertTotalQuantityAsync("LOT-A", 500, confirm: false, updatedByUserName: null));

        Assert.Contains("Line 1", ex.Message);
        Assert.Contains("Lắp ráp", ex.Message);
        _lotRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Lot>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-21a AC3: cùng tình huống trên nhưng đã Confirm = true -> cho phép ghi đè (soft-confirm, không chặn cứng).
    [Fact]
    public async Task UpsertTotalQuantityAsync_GiamDuoiThucTeDaConfirm_ChoPhepGhiDe()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LotSummaryDto
            {
                Lot = "LOT-A",
                Rows = new List<LotStageRowDto>
                {
                    new() { LineId = 1, LineName = "Line 1", StageId = 10, StageName = "Lắp ráp", OkCount = 800 },
                },
            });

        var result = await _sut.UpsertTotalQuantityAsync("LOT-A", 500, confirm: true, updatedByUserName: null);

        Assert.Equal(500, result.TotalQuantity);
    }

    // US-21a AC3: tăng/giữ nguyên (không vi phạm OkCount) -> không cần Confirm dù OkCount đã có dữ liệu.
    [Fact]
    public async Task UpsertTotalQuantityAsync_KhongViPham_KhongCanConfirm()
    {
        _lotReportServiceMock
            .Setup(s => s.GetLotSummaryAsync("LOT-A", null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LotSummaryDto
            {
                Lot = "LOT-A",
                Rows = new List<LotStageRowDto>
                {
                    new() { LineId = 1, LineName = "Line 1", StageId = 10, StageName = "Lắp ráp", OkCount = 800 },
                },
            });

        var result = await _sut.UpsertTotalQuantityAsync("LOT-A", 1000, confirm: false, updatedByUserName: null);

        Assert.Equal(1000, result.TotalQuantity);
    }
}
