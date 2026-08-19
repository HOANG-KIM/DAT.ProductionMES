using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="LotReportService"/> (US-21 vòng 3, AC1-AC5 — Lot-centric).
/// </summary>
public class LotReportServiceTests
{
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<Lot>> _lotRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly LotReportService _sut;

    public LotReportServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Lot>()).Returns(_lotRepositoryMock.Object);

        _sut = new LotReportService(_unitOfWorkMock.Object);

        SetupPlans(new List<ProductionPlan>());
        SetupPlanStages(new List<ProductionPlanStage>());
        SetupScans(new List<Scan>());
        SetupLines(new List<Line>());
        SetupStages(new List<Stage>());
        SetupLots(new List<Lot>());
    }

    private void SetupPlans(List<ProductionPlan> plans) =>
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlan, bool>> predicate, CancellationToken _) => plans.Where(predicate.Compile()).ToList());

    private void SetupPlanStages(List<ProductionPlanStage> planStages) =>
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) => planStages.Where(predicate.Compile()).ToList());

    private void SetupScans(List<Scan> scans) =>
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) => scans.Where(predicate.Compile()).ToList());

    private void SetupLines(List<Line> lines) =>
        _lineRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Line, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Line, bool>> predicate, CancellationToken _) => lines.Where(predicate.Compile()).ToList());

    private void SetupStages(List<Stage> stages) =>
        _stageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Stage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Stage, bool>> predicate, CancellationToken _) => stages.Where(predicate.Compile()).ToList());

    private void SetupLots(List<Lot> lots) =>
        _lotRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Lot, bool>> predicate, CancellationToken _) => lots.Where(predicate.Compile()).ToList());

    // AC1/AC2: tìm Lot khớp gần đúng, không phân biệt hoa/thường, giới hạn kết quả trùng lặp.
    [Fact]
    public async Task SearchLotsAsync_GoMotPhanMaLot_TraVeCacLotKhopGanDung()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, Lot = "LOT-2026-001", Model = "M1", Customer = "C1" },
            new() { Id = 2, Lot = "LOT-2026-002", Model = "M1", Customer = "C1" },
            new() { Id = 3, Lot = "LOT-2026-001", Model = "M2", Customer = "C2" }, // Cùng Lot với plan 1, khác Line/Model.
            new() { Id = 4, Lot = "OTHER-LOT", Model = "M3", Customer = "C3" },
        });

        var result = await _sut.SearchLotsAsync("2026");

        Assert.Equal(2, result.Count); // Distinct: LOT-2026-001, LOT-2026-002.
        Assert.Contains(result, r => r.Lot == "LOT-2026-001");
        Assert.Contains(result, r => r.Lot == "LOT-2026-002");
    }

    // AC2: search rỗng -> không gọi query, trả về rỗng ngay (tránh liệt kê toàn bộ Lot khi ô tìm kiếm trống).
    [Fact]
    public async Task SearchLotsAsync_SearchRong_TraVeDanhSachRong()
    {
        var result = await _sut.SearchLotsAsync("   ");

        Assert.Empty(result);
    }

    // AC2: Lot không khớp bất kỳ ProductionPlan nào -> GetLotSummaryAsync trả về null, KHÔNG throw exception.
    [Fact]
    public async Task GetLotSummaryAsync_LotKhongTonTai_TraVeNull()
    {
        var result = await _sut.GetLotSummaryAsync("LOT-KHONG-TON-TAI", null, null);

        Assert.Null(result);
    }

    // AC3: mọi kế hoạch cùng Lot có Model/Customer/Revision giống hệt -> hiển thị đúng 1 giá trị duy nhất mỗi field.
    [Fact]
    public async Task GetLotSummaryAsync_MoiKeHoachCungLotDongNhat_TraVeMotGiaTriDuyNhat()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", Revision = "R1" },
            new() { Id = 2, LineId = 2, Lot = "LOT-A", Model = "M1", Customer = "C1", Revision = "R1" },
        });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        Assert.Equal(new[] { "M1" }, result!.Models);
        Assert.Equal(new[] { "C1" }, result.Customers);
        Assert.Equal(new[] { "R1" }, result.Revisions);
    }

    // AC3: các kế hoạch cùng Lot có Model KHÁC NHAU (thường do khác Line) -> hiển thị TẤT CẢ giá trị khác nhau, không tự chọn đại diện.
    [Fact]
    public async Task GetLotSummaryAsync_CacKeHoachCungLotKhacModel_TraVeTatCaGiaTriKhongDongNhat()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", Revision = "R1" },
            new() { Id = 2, LineId = 2, Lot = "LOT-A", Model = "M2", Customer = "C1", Revision = "R1" },
        });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Models.Count); // Không đồng nhất -> 2 giá trị, cảnh báo do client tự xử lý.
        Assert.Contains("M1", result.Models);
        Assert.Contains("M2", result.Models);
    }

    // AC4: gộp (Line, Công đoạn) từ ProductionPlanStage (không giới hạn PlanStatus) + đếm OK/NG snapshot trên Scan.Lot.
    [Fact]
    public async Task GetLotSummaryAsync_CoKeHoachVaLuotScan_TraVeDungDongLineCongDoanKemOkNg()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", Revision = "R1" },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, LineId = 1, StageId = 10, PlanStatus = PlanStatus.Completed },
        });
        SetupScans(new List<Scan>
        {
            new() { Id = 1, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc) },
            new() { Id = 2, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = new DateTime(2026, 8, 18, 8, 5, 0, DateTimeKind.Utc) },
            new() { Id = 3, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ng, ScannedAtUtc = new DateTime(2026, 8, 18, 8, 10, 0, DateTimeKind.Utc) },
            // Kết quả hệ thống tự động (lỗi thao tác) KHÔNG tính vào NG/OK (cùng quy tắc US-09/US-18).
            new() { Id = 4, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.DuplicateTag, ScannedAtUtc = new DateTime(2026, 8, 18, 8, 15, 0, DateTimeKind.Utc) },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 10, Name = "Lắp ráp" } });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        var row = Assert.Single(result!.Rows);
        Assert.Equal(1, row.LineId);
        Assert.Equal("Line 1", row.LineName);
        Assert.Equal(10, row.StageId);
        Assert.Equal("Lắp ráp", row.StageName);
        Assert.Equal(2, row.OkCount);
        Assert.Equal(1, row.NgCount);
    }

    // AC4: (Line, Công đoạn) có lượt scan nhưng KHÔNG (còn) có ProductionPlanStage nào cấu hình -> vẫn hiển thị dòng (lưới an toàn union).
    [Fact]
    public async Task GetLotSummaryAsync_ChiCoLuotScanKhongConProductionPlanStage_VanHienThiDong()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1" },
        });
        // Không setup ProductionPlanStage nào -> rỗng.
        SetupScans(new List<Scan>
        {
            new() { Id = 1, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 10, Name = "Lắp ráp" } });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        var row = Assert.Single(result!.Rows);
        Assert.Equal(1, row.OkCount);
    }

    // US-21a AC5 (viết lại hoàn toàn 19/08/2026): "Tổng số lượng Lot" đọc từ entity Lot (nhập tay), KHÔNG phải SUM(PlannedQuantity).
    [Fact]
    public async Task GetLotSummaryAsync_LotDaCoTongSoLuongNhapTay_TraVeDungGiaTri()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", PlannedQuantity = 1000 },
            new() { Id = 2, LineId = 2, Lot = "LOT-A", Model = "M1", Customer = "C1", PlannedQuantity = 500 },
        });
        SetupLots(new List<Lot> { new() { Id = 1, Code = "LOT-A", TotalQuantity = 1800 } });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        Assert.Equal(1800, result!.LotTotalQuantity); // KHÔNG phải SUM(1000+500) = 1500.
    }

    // US-21a AC6: Lot CHƯA từng có ai nhập "Tổng số lượng Lot" -> null ("Chưa xác định"), không suy luận từ PlannedQuantity.
    [Fact]
    public async Task GetLotSummaryAsync_LotChuaXacDinh_TraVeNull()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", PlannedQuantity = 1000 },
        });
        // Mặc định constructor: SetupLots(rỗng) -> chưa có ai nhập.

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        Assert.Null(result!.LotTotalQuantity);
    }

    // US-21a AC5: mỗi dòng (Line, Công đoạn) so sánh OkCount với LotTotalQuantity riêng biệt -> "Đủ"/"Chưa đủ" theo TỪNG dòng.
    [Fact]
    public async Task GetLotSummaryAsync_LotDaCoTongSoLuong_MoiDongSoSanhOkCountRiengBiet()
    {
        SetupPlans(new List<ProductionPlan> { new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, LineId = 1, StageId = 10, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 1, LineId = 1, StageId = 20, PlanStatus = PlanStatus.Running },
        });
        SetupScans(new List<Scan>
        {
            new() { Id = 1, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { Id = 2, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
            new() { Id = 3, LineId = 1, StageId = 20, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = DateTime.UtcNow },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 10, Name = "Lắp ráp" }, new() { Id = 20, Name = "Đóng gói" } });
        SetupLots(new List<Lot> { new() { Id = 1, Code = "LOT-A", TotalQuantity = 2 } }); // Đủ cho Stage 10 (2 OK), chưa đủ Stage 20 (1 OK).

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        var stage10Row = result!.Rows.Single(r => r.StageId == 10);
        var stage20Row = result.Rows.Single(r => r.StageId == 20);
        Assert.True(stage10Row.IsSufficientQuantity);
        Assert.False(stage20Row.IsSufficientQuantity);
    }

    // US-21a AC1: vẫn hoạt động đúng dù (Line, Công đoạn) chưa từng có ProductionPlanStage/Scan nào (Rows rỗng).
    [Fact]
    public async Task GetLotSummaryAsync_ChuaCoPlanStageHoacScanNao_VanTraVeDungLotTotalQuantity()
    {
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1", PlannedQuantity = 1000 },
        });
        SetupLots(new List<Lot> { new() { Id = 1, Code = "LOT-A", TotalQuantity = 1000 } });

        var result = await _sut.GetLotSummaryAsync("LOT-A", null, null);

        Assert.NotNull(result);
        Assert.Empty(result!.Rows);
        Assert.Equal(1000, result.LotTotalQuantity);
    }

    // AC5: lọc theo khoảng thời gian -> chỉ đếm lượt scan trong khoảng, không tính lượt ngoài khoảng.
    [Fact]
    public async Task GetLotSummaryAsync_LocTheoKhoangThoiGian_ChiDemLuotScanTrongKhoang()
    {
        var inRange = new DateTime(2026, 8, 18, 9, 0, 0, DateTimeKind.Utc);
        var outOfRange = new DateTime(2026, 8, 18, 7, 0, 0, DateTimeKind.Utc);

        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 1, LineId = 1, Lot = "LOT-A", Model = "M1", Customer = "C1" },
        });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 1, LineId = 1, StageId = 10, PlanStatus = PlanStatus.Running },
        });
        SetupScans(new List<Scan>
        {
            new() { Id = 1, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = outOfRange },
            new() { Id = 2, LineId = 1, StageId = 10, Lot = "LOT-A", Result = ScanResult.Ok, ScannedAtUtc = inRange },
        });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupStages(new List<Stage> { new() { Id = 10, Name = "Lắp ráp" } });

        var result = await _sut.GetLotSummaryAsync("LOT-A", fromUtc: new DateTime(2026, 8, 18, 8, 30, 0, DateTimeKind.Utc), toUtc: null);

        Assert.NotNull(result);
        var row = Assert.Single(result!.Rows);
        Assert.Equal(1, row.OkCount); // Chỉ đếm lượt inRange.
    }
}
