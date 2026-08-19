using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Lots;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.Services.Lots;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ProductionPlanStageService, bám theo AC1-AC7 của US-05a (Documents/BACKLOG-user-story.md).
/// Trình tự công đoạn (US-03) đã chuyển hẳn sang LineStageSequenceService (xem LineStageSequenceServiceTests) —
/// các test dưới đây setup mock LineStageSequence thay vì set SequenceNumber trực tiếp trên ProductionPlanStage
/// (đã bỏ property này, xem remarks tại entity), phản ánh đúng cơ chế "lazy get-or-create" mới.
/// </summary>
public class ProductionPlanStageServiceTests
{
    private readonly Mock<IRepository<ProductionPlanStage>> _repositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<LineStageSequence>> _lineStageSequenceRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ILotService> _lotServiceMock = new();
    private readonly ProductionPlanStageService _sut;

    public ProductionPlanStageServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_repositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<LineStageSequence>()).Returns(_lineStageSequenceRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _lotServiceMock
            .Setup(s => s.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, LotDto>());
        _sut = new ProductionPlanStageService(_unitOfWorkMock.Object, _lotServiceMock.Object);

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = 1, LineId = 1, PlannedQuantity = 1000 });

        // Mặc định không có lượt scan OK nào — từng test override qua SetupOkScans khi cần kiểm tra RunCount/RemainingCount.
        SetupOkScans(new List<Scan>());
        // Mặc định trình tự Line trống — từng test override qua SetupLineSequence.
        SetupLineSequence(new List<LineStageSequence>());
    }

    private void SetupOkScans(List<Scan> okScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                okScans.Where(predicate.Compile()).ToList());
    }

    private void SetupPlanStages(List<ProductionPlanStage> items)
    {
        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    private void SetupProductionPlans(List<ProductionPlan> plans)
    {
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlan, bool>> predicate, CancellationToken _) =>
                plans.Where(predicate.Compile()).ToList());
    }

    private void SetupLineSequence(List<LineStageSequence> items)
    {
        _lineStageSequenceRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LineStageSequence, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<LineStageSequence, bool>> predicate, CancellationToken _) =>
                items.Where(predicate.Compile()).ToList());
    }

    // US-05a AC4 — "Đã chạy"/"còn lại" tính động từ lịch sử scan OK, không đọc từ cột số liệu tĩnh.
    [Fact]
    public async Task GetByProductionPlanAsync_DaCo400ScanOk_TraVeDaChay400ConLai600()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var existing = new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Paused },
        };
        SetupPlanStages(existing);
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList());

        var result = await _sut.GetByProductionPlanAsync(1);

        var stage = result.Single();
        Assert.Equal(400, stage.RunCount);
        Assert.Equal(600, stage.RemainingCount);
        Assert.Equal(1, stage.SequenceNumber);
        Assert.Null(stage.PreviousStageId);
    }

    // Cơ chế "lazy get-or-create": trình tự Line có 2 công đoạn nhưng kế hoạch chưa có bản ghi vòng đời nào ->
    // tự động tạo mới 2 bản ghi Draft, trả về ĐẦY ĐỦ theo trình tự Line.
    [Fact]
    public async Task GetByProductionPlanAsync_ChuaCoBanGhiVongDoiNao_TuDongTaoDraftChoDuTrinhTuLine()
    {
        SetupLineSequence(new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        });
        SetupPlanStages(new List<ProductionPlanStage>());

        var result = await _sut.GetByProductionPlanAsync(1);

        Assert.Equal(2, result.Count);
        Assert.All(result, x => Assert.Equal(PlanStatus.Draft, x.PlanStatus));
        var stage10 = result.Single(x => x.StageId == 10);
        var stage20 = result.Single(x => x.StageId == 20);
        Assert.Equal(1, stage10.SequenceNumber);
        Assert.Null(stage10.PreviousStageId);
        Assert.Equal(2, stage20.SequenceNumber);
        Assert.Equal(10, stage20.PreviousStageId);
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<ProductionPlanStage>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // US-05a AC1 — Áp dụng kế hoạch (Draft) cho công đoạn, chưa có kế hoạch khác Running cùng (Line, Công đoạn) -> Running.
    [Fact]
    public async Task ApplyAsync_ChuaCoKeHoachKhacRunning_ChuyenRunningThanhCong()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Draft };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        var result = await _sut.ApplyAsync(1, 10);

        Assert.Equal(PlanStatus.Running, result.PlanStatus);
        Assert.Equal(PlanStatus.Running, item.PlanStatus);
        _repositoryMock.Verify(r => r.Update(item), Times.Once);
    }

    // Công đoạn không thuộc trình tự đã cấu hình của Line -> từ chối, không tự tạo bản ghi vòng đời "ảo".
    [Fact]
    public async Task ApplyAsync_CongDoanKhongThuocTrinhTuLine_NemEntityNotFoundException()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 99, SequenceNumber = 1 } });
        SetupPlanStages(new List<ProductionPlanStage>());

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.ApplyAsync(1, 10));
    }

    // US-05a AC1 — (Line, Công đoạn) đang có kế hoạch KHÁC Running -> từ chối, yêu cầu Tạm dừng/Đóng trước.
    [Fact]
    public async Task ApplyAsync_LineCongDoanDangCoKeHoachKhacRunning_NemBusinessRuleException()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 2, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Draft };
        var runningOther = new ProductionPlanStage { Id = 1, ProductionPlanId = 99, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running };

        // Repository FindAsync trả về dữ liệu tùy theo productionPlanId trong predicate — mô phỏng 2 truy vấn khác nhau
        // trong ApplyAsync: 1) lấy item theo (ProductionPlanId=1), 2) tìm Running theo (LineId, StageId) toàn hệ thống.
        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { item, runningOther }.Where(predicate.Compile()).ToList());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ApplyAsync(1, 10));
        Assert.Equal(PlanStatus.Draft, item.PlanStatus); // không đổi trạng thái khi bị từ chối
    }

    // US-05a AC2 — Công đoạn KHÁC của cùng Line được áp dụng kế hoạch khác song song, không bị chặn.
    [Fact]
    public async Task ApplyAsync_CongDoanKhacCungLineDangRunning_KhongBiChan()
    {
        SetupLineSequence(new List<LineStageSequence>
        {
            new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 },
            new() { Id = 2, LineId = 1, StageId = 20, SequenceNumber = 2 },
        });
        var item = new ProductionPlanStage { Id = 2, ProductionPlanId = 1, StageId = 20, LineId = 1, PlanStatus = PlanStatus.Draft };
        var runningStageA = new ProductionPlanStage { Id = 1, ProductionPlanId = 99, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running };

        _repositoryMock
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((System.Linq.Expressions.Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { item, runningStageA }.Where(predicate.Compile()).ToList());

        var result = await _sut.ApplyAsync(1, 20);

        Assert.Equal(PlanStatus.Running, result.PlanStatus);
    }

    // US-05a AC7 — Completed/Cancelled không tự "Áp dụng" lại được.
    [Theory]
    [InlineData(PlanStatus.Completed)]
    [InlineData(PlanStatus.Cancelled)]
    public async Task ApplyAsync_DaCompletedHoacCancelled_NemBusinessRuleException(PlanStatus status)
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = status };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ApplyAsync(1, 10));
    }

    // US-05a AC3 — Tạm dừng: Running -> Paused, giữ nguyên (không xóa) tiến độ vì tính động.
    [Fact]
    public async Task PauseAsync_DangRunning_ChuyenPausedThanhCong()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        var result = await _sut.PauseAsync(1, 10);

        Assert.Equal(PlanStatus.Paused, result.PlanStatus);
        _repositoryMock.Verify(r => r.Update(item), Times.Once);
    }

    [Fact]
    public async Task PauseAsync_KhongPhaiRunning_NemBusinessRuleException()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Draft };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.PauseAsync(1, 10));
    }

    // US-05a AC6 — Đóng sớm khi chưa đủ số lượng mà chưa Confirm -> từ chối, nêu rõ số lượng còn thiếu.
    [Fact]
    public async Task CloseAsync_ChuaDuSoLuongChuaConfirm_NemBusinessRuleException()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList()); // 400/1000

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = false }));
        Assert.Equal(PlanStatus.Running, item.PlanStatus); // chưa đổi trạng thái
    }

    // US-05a AC6 — Đóng sớm khi chưa đủ số lượng, đã Confirm -> chuyển Cancelled.
    [Fact]
    public async Task CloseAsync_ChuaDuSoLuongDaConfirm_ChuyenCancelledThanhCong()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running };
        SetupPlanStages(new List<ProductionPlanStage> { item });
        SetupOkScans(Enumerable.Range(1, 400)
            .Select(i => new Scan { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = $"TAG{i}" })
            .ToList());

        var result = await _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = true });

        Assert.Equal(PlanStatus.Cancelled, result.PlanStatus);
        Assert.Equal(PlanStatus.Cancelled, item.PlanStatus);
    }

    [Fact]
    public async Task CloseAsync_DaCompleted_NemBusinessRuleException()
    {
        SetupLineSequence(new List<LineStageSequence> { new() { Id = 1, LineId = 1, StageId = 10, SequenceNumber = 1 } });
        var item = new ProductionPlanStage { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Completed };
        SetupPlanStages(new List<ProductionPlanStage> { item });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CloseAsync(1, 10, new CloseProductionPlanStageRequest { Confirm = true }));
    }

    // US-03 (17/08/2026) — mọi kế hoạch trên Line tự động áp dụng mọi Stage trong trình tự Line: liệt kê MỌI
    // ProductionPlan có LineId khớp, mặc định ẩn các cặp đã Completed/Cancelled tại đúng công đoạn đang lọc.
    [Fact]
    public async Task GetByLineAndStageAsync_MacDinh_AnKeHoachDaCompletedHoacCancelled()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Customer = "Samsung", PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
            new() { Id = 2, LineId = 1, Customer = "Samsung", PlannedQuantity = 500, TaktTimeSeconds = 9, StartTime = new DateTime(2026, 8, 1) },
            new() { Id = 3, LineId = 1, Customer = "LG", PlannedQuantity = 500, TaktTimeSeconds = 9, StartTime = new DateTime(2026, 8, 2) },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 2, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Completed },
            new() { Id = 3, ProductionPlanId = 3, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Cancelled },
        });

        var result = await _sut.GetByLineAndStageAsync(1, 10);

        var plan = Assert.Single(result);
        Assert.Equal(1, plan.ProductionPlanId);
    }

    // US-05b AC2 — includeClosed = true hiển thị đầy đủ, kể cả Completed/Cancelled.
    [Fact]
    public async Task GetByLineAndStageAsync_IncludeClosedTrue_HienDayDuKeCaDaDong()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
            new() { Id = 2, LineId = 1, PlannedQuantity = 500, TaktTimeSeconds = 9, StartTime = new DateTime(2026, 8, 1) },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 2, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Completed },
        });

        var result = await _sut.GetByLineAndStageAsync(1, 10, includeClosed: true);

        Assert.Equal(2, result.Count);
    }

    // US-03 — kế hoạch trên Line tự động áp dụng mọi Stage trong trình tự Line kể cả khi CHƯA có bản ghi vòng
    // đời thật nào được persist (chưa từng Áp dụng/Tạm dừng/Đóng) -> mặc định hiển thị Draft, không lỗi.
    [Fact]
    public async Task GetByLineAndStageAsync_ChuaCoBanGhiVongDoiThat_MacDinhDraft()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
        });
        SetupPlanStages(new List<ProductionPlanStage>());

        var result = await _sut.GetByLineAndStageAsync(1, 10);

        var plan = Assert.Single(result);
        Assert.Equal(PlanStatus.Draft, plan.PlanStatus);
        Assert.Equal(0, plan.RunCount);
    }

    // US-05a AC4 — RunCount tính động, đúng theo StageId đang lọc, không lẫn lượt scan ở công đoạn khác của cùng kế hoạch.
    [Fact]
    public async Task GetByLineAndStageAsync_CoScanOkODungCongDoan_TinhRunCountVaRemainingCountDung()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
        });
        SetupOkScans(new List<Scan>
        {
            new() { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = "TAG1" },
            new() { ProductionPlanId = 1, StageId = 10, Result = ScanResult.Ok, TagCode = "TAG2" },
            // Lượt scan OK ở công đoạn KHÁC (StageId = 20) của cùng kế hoạch — không được tính vào RunCount ở đây.
            new() { ProductionPlanId = 1, StageId = 20, Result = ScanResult.Ok, TagCode = "TAG3" },
        });

        var result = await _sut.GetByLineAndStageAsync(1, 10);

        var plan = Assert.Single(result);
        Assert.Equal(2, plan.RunCount);
        Assert.Equal(998, plan.RemainingCount);
        Assert.Equal(300, plan.StandardQuantityPerHour); // 3600 / 12
    }

    // US-05b — Line chưa có kế hoạch nào -> trả về danh sách rỗng, không lỗi.
    [Fact]
    public async Task GetByLineAndStageAsync_ChuaCoKeHoachNaoTrenLine_TraVeDanhSachRong()
    {
        SetupProductionPlans(new List<ProductionPlan>());
        SetupPlanStages(new List<ProductionPlanStage>());

        var result = await _sut.GetByLineAndStageAsync(1, 99);

        Assert.Empty(result);
    }

    // US-21a AC7 (viết lại hoàn toàn 19/08/2026): "Tổng SL Lot" đọc từ entity Lot theo Code (nhập tay), KHÔNG
    // phải SUM(PlannedQuantity) — tra theo tập hợp Code xuất hiện trong danh sách kế hoạch của Line đang xem.
    [Fact]
    public async Task GetByLineAndStageAsync_LotDaCoTongSoLuongNhapTay_TraVeDungGiaTriDaNhap()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
        });
        _lotServiceMock
            .Setup(s => s.GetByCodesAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, LotDto> { ["LOT-A"] = new LotDto { Code = "LOT-A", TotalQuantity = 1500 } });

        var result = await _sut.GetByLineAndStageAsync(1, 10);

        var plan = Assert.Single(result);
        Assert.Equal(1, plan.ProductionPlanId);
        Assert.Equal(1000, plan.PlannedQuantity);
        Assert.Equal(1500, plan.LotTotalQuantity);
    }

    // US-21a AC6: Lot CHƯA từng có ai nhập "Tổng số lượng Lot" -> null ("Chưa xác định"), không suy luận từ PlannedQuantity.
    [Fact]
    public async Task GetByLineAndStageAsync_LotChuaXacDinh_TraVeNull()
    {
        SetupProductionPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", PlannedQuantity = 1000, TaktTimeSeconds = 12, StartTime = new DateTime(2026, 8, 14) },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, StageId = 10, LineId = 1, PlanStatus = PlanStatus.Running },
        });
        // Mặc định constructor: GetByCodesAsync trả về dictionary rỗng (không có Lot nào được nhập).

        var result = await _sut.GetByLineAndStageAsync(1, 10);

        var plan = Assert.Single(result);
        Assert.Null(plan.LotTotalQuantity);
    }
}
