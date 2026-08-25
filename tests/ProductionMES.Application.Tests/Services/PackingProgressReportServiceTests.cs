using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.DTOs.Reports;
using ProductionMES.Application.Services.Reports;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="PackingProgressReportService"/> (US-26/FR-26) — AC1 (gợi ý/autocomplete Lot đang
/// Running tại công đoạn Đóng thùng, viết lại 25/08/2026 sang mô hình tra cứu theo Lot), AC2 (dòng kết quả tra
/// cứu ứng với kế hoạch đang Running tại công đoạn Đóng thùng), AC3 ("Chưa xác định" khi Lot chưa có Tổng số
/// lượng), AC4 (lọc Line/Lot/Model, gộp nhiều ProductionPlanId cùng Lot khi kế hoạch cũ bị Cancelled rồi tạo lại,
/// hiển thị đầy đủ khi 1 Lot chạy nhiều Line), AC6 (danh sách thùng chi tiết theo dòng báo cáo), AC7/AC8 (chi tiết
/// lượt scan trong 1 thùng, giới hạn dữ liệu lịch sử).
/// </summary>
public class PackingProgressReportServiceTests
{
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlanStage>> _productionPlanStageRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Line>> _lineRepositoryMock = new();
    private readonly Mock<IRepository<PackingBox>> _packingBoxRepositoryMock = new();
    private readonly Mock<IRepository<Lot>> _lotRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PackingProgressReportService _sut;

    public PackingProgressReportServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Line>()).Returns(_lineRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<PackingBox>()).Returns(_packingBoxRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Lot>()).Returns(_lotRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);

        _sut = new PackingProgressReportService(_unitOfWorkMock.Object);

        SetupStages(new List<Stage>());
        SetupPlanStages(new List<ProductionPlanStage>());
        SetupPlans(new List<ProductionPlan>());
        SetupLines(new List<Line>());
        SetupPackingBoxes(new List<PackingBox>());
        SetupLots(new List<Lot>());
        SetupScans(new List<Scan>());
    }

    private void SetupStages(List<Stage> stages) =>
        _stageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Stage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Stage, bool>> predicate, CancellationToken _) => stages.Where(predicate.Compile()).ToList());

    private void SetupPlanStages(List<ProductionPlanStage> planStages) =>
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) => planStages.Where(predicate.Compile()).ToList());

    private void SetupPlans(List<ProductionPlan> plans) =>
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlan, bool>> predicate, CancellationToken _) => plans.Where(predicate.Compile()).ToList());

    private void SetupLines(List<Line> lines) =>
        _lineRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Line, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Line, bool>> predicate, CancellationToken _) => lines.Where(predicate.Compile()).ToList());

    private void SetupPackingBoxes(List<PackingBox> boxes) =>
        _packingBoxRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<PackingBox, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<PackingBox, bool>> predicate, CancellationToken _) => boxes.Where(predicate.Compile()).ToList());

    private void SetupLots(List<Lot> lots) =>
        _lotRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Lot, bool>> predicate, CancellationToken _) => lots.Where(predicate.Compile()).ToList());

    private void SetupScans(List<Scan> scans) =>
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) => scans.Where(predicate.Compile()).ToList());

    private void SetupPackingBoxById(PackingBox? box) =>
        _packingBoxRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(box);

    // AC1: ô tìm kiếm trống -> trả rỗng, KHÔNG truy vấn/hiển thị bất kỳ dữ liệu nào.
    [Fact]
    public async Task SearchAsync_TimKiemRong_TraVeRong()
    {
        var result = await _sut.SearchAsync(string.Empty);

        Assert.Empty(result);
    }

    // AC1 (viết lại LẦN 2): khớp 1 phần mã Lot của kế hoạch đang Running tại công đoạn Đóng thùng -> trả về gợi ý
    // CHỈ gồm mã Lot (không còn kèm Line/Model).
    [Fact]
    public async Task SearchAsync_AC1_KhopMotPhanMaLotDangRunningTaiDongThung_TraVeGoiYChiGomMaLot()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-ABC" } });

        var result = await _sut.SearchAsync("LOT-AB");

        var item = Assert.Single(result);
        Assert.Equal("LOT-ABC", item.Lot);
    }

    // AC1 (viết lại LẦN 3 — 25/08/2026): Lot CHƯA từng "Áp dụng" (chỉ có ProductionPlanStage Draft) -> KHÔNG được gợi ý.
    [Fact]
    public async Task SearchAsync_AC1_LotChiCoDraft_KhongDuocGoiY()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            // Chưa từng "Áp dụng" -> chắc chắn chưa có dữ liệu đóng thùng nào -> không gợi ý.
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Draft },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-ABC" } });

        var result = await _sut.SearchAsync("LOT-AB");

        Assert.Empty(result);
    }

    // AC1 (viết lại LẦN 3 — 25/08/2026): Lot đang Paused/Completed/Cancelled tại Đóng thùng (không chỉ Running)
    // -> VẪN được gợi ý (mở rộng phạm vi PlanStatus, giải quyết gap "Lot Paused/Completed/Cancelled tra cứu ở đâu?").
    [Theory]
    [InlineData(PlanStatus.Paused)]
    [InlineData(PlanStatus.Completed)]
    [InlineData(PlanStatus.Cancelled)]
    public async Task SearchAsync_AC1_LotOTrangThaiKhacDraft_VanDuocGoiY(PlanStatus planStatus)
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = planStatus },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-ABC" } });

        var result = await _sut.SearchAsync("LOT-AB");

        var item = Assert.Single(result);
        Assert.Equal("LOT-ABC", item.Lot);
    }

    // AC1: Running nhưng KHÔNG phải công đoạn Đóng thùng -> KHÔNG được gợi ý.
    [Fact]
    public async Task SearchAsync_AC1_RunningNhungKhongPhaiCongDoanDongThung_KhongDuocGoiY()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp", IsPackingStage = false } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-ABC" } });

        var result = await _sut.SearchAsync("LOT-AB");

        Assert.Empty(result);
    }

    // AC1 (viết lại LẦN 2): dedupe theo Lot — cùng Lot đang Running đồng thời trên nhiều Line -> CHỈ trả về đúng 1
    // gợi ý duy nhất cho Lot đó (không lặp lại theo Line).
    [Fact]
    public async Task SearchAsync_AC1_CungLotChayDongThoiNhieuLine_GopVeDungMotGoiYDuyNhat()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 11, LineId = 2, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 11, LineId = 2, Model = "M2", Lot = "LOT-X" },
        });

        var result = await _sut.SearchAsync("LOT-X");

        var item = Assert.Single(result);
        Assert.Equal("LOT-X", item.Lot);
    }

    // Chưa có Stage nào được đánh dấu IsPackingStage -> báo cáo rỗng, không lỗi.
    [Fact]
    public async Task GetReportAsync_ChuaCoCongDoanDongThung_TraVeRowsRong()
    {
        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        Assert.Empty(result.Rows);
    }

    // AC2: kế hoạch đang Running tại công đoạn Đóng thùng, đã đóng xong 2 thùng (Completed) -> hiển thị đúng
    // Line/Model/Lot/số thùng/tổng số lượng OK. Thùng đang InProgress dở KHÔNG được cộng vào (AC2).
    [Fact]
    public async Task GetReportAsync_AC2_KeHoachDangRunningTaiDongThung_TraVeDungSoThungVaSoLuong()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20 },
            new() { Id = 2, ProductionPlanId = 10, StageId = 100, BoxNo = 2, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20 },
            // Thùng đang dở -> KHÔNG cộng vào (AC1).
            new() { Id = 3, ProductionPlanId = 10, StageId = 100, BoxNo = 3, Status = PackingBoxStatus.InProgress, TargetQuantity = 20, ScannedQuantity = 5 },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.LineId);
        Assert.Equal("Line 1", row.LineName);
        Assert.Equal("M1", row.Model);
        Assert.Equal("LOT-A", row.Lot);
        Assert.Equal(2, row.CompletedBoxCount);
        Assert.Equal(40, row.PackedOkQuantity);
    }

    // AC1/AC14 (viết lại LẦN 3 — 25/08/2026): (Line, Đóng thùng) chỉ toàn Draft (chưa từng "Áp dụng") -> KHÔNG
    // hiển thị dòng nào (khác US-21, không có placeholder) — Draft chắc chắn chưa có dữ liệu đóng thùng.
    [Fact]
    public async Task GetReportAsync_ChiCoKeHoachDraftTaiDongThung_KhongHienThiDong()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Draft },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        Assert.Empty(result.Rows);
    }

    // AC2/AC14 (viết lại LẦN 3 — 25/08/2026): (Line, Đóng thùng) có kế hoạch ở Paused/Completed/Cancelled (không
    // chỉ Running) -> VẪN hiển thị dòng, cột PlanStatus đúng trạng thái của kế hoạch đó.
    [Theory]
    [InlineData(PlanStatus.Paused)]
    [InlineData(PlanStatus.Completed)]
    [InlineData(PlanStatus.Cancelled)]
    public async Task GetReportAsync_AC14_KeHoachOTrangThaiKhacDraft_VanHienThiDongDungPlanStatus(PlanStatus planStatus)
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = planStatus },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(planStatus, row.PlanStatus);
        Assert.Equal("LOT-A", row.Lot);
    }

    // AC14: 1 cặp (Line, Đóng thùng) có NHIỀU ProductionPlanStage lịch sử cùng Lot ở các PlanStatus khác nhau ->
    // chọn đúng bản ghi đại diện theo thứ tự ưu tiên Running > Paused > Completed > Cancelled.
    [Fact]
    public async Task GetReportAsync_AC14_NhieuLichSuCungLot_ChonDungBanGhiDaiDienTheoThuTuUuTien()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
            new() { Id = 2, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Paused },
            new() { Id = 3, ProductionPlanId = 12, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Completed },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 11, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 12, LineId = 1, Model = "M1", Lot = "LOT-X" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        // Paused ưu tiên hơn Completed/Cancelled -> chọn ProductionPlanId = 11.
        var row = Assert.Single(result.Rows);
        Assert.Equal(11, row.ProductionPlanId);
        Assert.Equal(PlanStatus.Paused, row.PlanStatus);
    }

    // AC14: nhiều bản ghi CÙNG mức ưu tiên (2 lần Cancelled) -> chọn bản ghi Id lớn nhất (gần nhất).
    [Fact]
    public async Task GetReportAsync_AC14_NhieuBanGhiCungMucUuTien_ChonIdLonNhat()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
            new() { Id = 5, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 11, LineId = 1, Model = "M1", Lot = "LOT-X" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(11, row.ProductionPlanId); // ProductionPlanStage.Id = 5 lớn hơn -> đại diện.
    }

    // Công đoạn KHÔNG phải Đóng thùng (IsPackingStage = false) dù đang Running cũng không được liệt kê.
    [Fact]
    public async Task GetReportAsync_CongDoanKhongPhaiDongThung_KhongHienThiDong()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Lắp ráp", IsPackingStage = false } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        Assert.Empty(result.Rows);
    }

    // AC2: Lot đã có "Tổng số lượng Lot" -> % hoàn thành + nhãn Đủ/Chưa đủ tính đúng (dùng lại quy ước US-21a).
    [Fact]
    public async Task GetReportAsync_AC2_LotDaCoTongSoLuong_TinhDungPhanTramVaNhanDuChuaDu()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 30, ScannedQuantity = 30 },
        });
        SetupLots(new List<Lot> { new() { Code = "LOT-A", TotalQuantity = 100 } });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(100, row.LotTotalQuantity);
        Assert.Equal(30m, row.CompletionPercentage);
        Assert.False(row.IsSufficientQuantity);

        // Đủ số lượng (>=100%) -> nhãn "Đủ".
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 100, ScannedQuantity = 100 },
        });
        var resultDu = await _sut.GetReportAsync(new PackingProgressReportQuery());
        var rowDu = Assert.Single(resultDu.Rows);
        Assert.Equal(100m, rowDu.CompletionPercentage);
        Assert.True(rowDu.IsSufficientQuantity);
    }

    // AC3: Lot CHƯA từng nhập "Tổng số lượng Lot" -> LotTotalQuantity/CompletionPercentage/IsSufficientQuantity đều
    // null ("Chưa xác định"), KHÔNG suy diễn 0%.
    [Fact]
    public async Task GetReportAsync_AC3_LotChuaCoTongSoLuong_TraVeChuaXacDinh()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 30, ScannedQuantity = 30 },
        });
        // Không setup Lot nào -> lotsByCode rỗng.

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Null(row.LotTotalQuantity);
        Assert.Null(row.CompletionPercentage);
        Assert.Null(row.IsSufficientQuantity);
    }

    // AC4 (gộp theo Lot): kế hoạch cũ bị Cancelled rồi tạo lại kế hoạch mới cho CÙNG Lot tại CÙNG (Line, Đóng
    // thùng) -> số thùng/số lượng của dòng đang Running phải GỘP (SUM) cả thùng đã đóng dưới kế hoạch cũ.
    [Fact]
    public async Task GetReportAsync_AC4_CungLotKhacProductionPlanIdDoKeHoachCuBiHuy_GopChungSoThung()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
            new() { Id = 2, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 11, LineId = 1, Model = "M1", Lot = "LOT-X" },
        });
        SetupPackingBoxes(new List<PackingBox>
        {
            // Đóng dưới kế hoạch CŨ (đã Cancelled).
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20 },
            // Đóng dưới kế hoạch MỚI (đang Running).
            new() { Id = 2, ProductionPlanId = 11, StageId = 100, BoxNo = 2, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20 },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        var row = Assert.Single(result.Rows);
        Assert.Equal(11, row.ProductionPlanId); // Đại diện đúng kế hoạch đang Running.
        Assert.Equal("LOT-X", row.Lot);
        Assert.Equal(2, row.CompletedBoxCount);
        Assert.Equal(40, row.PackedOkQuantity);
    }

    // AC4: lọc theo LineId — chỉ trả về dòng của đúng Line được lọc.
    [Fact]
    public async Task GetReportAsync_AC4_LocTheoLineId_ChiTraVeDongCuaDungLine()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" },
            new() { Id = 20, LineId = 2, Model = "M2", Lot = "LOT-B" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery { LineId = 1 });

        var row = Assert.Single(result.Rows);
        Assert.Equal(1, row.LineId);
        Assert.Equal("LOT-A", row.Lot);
    }

    // AC4: lọc theo Lot — chỉ trả về dòng khớp đúng Lot.
    [Fact]
    public async Task GetReportAsync_AC4_LocTheoLot_ChiTraVeDongKhopLot()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" },
            new() { Id = 20, LineId = 2, Model = "M2", Lot = "LOT-B" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery { Lot = "LOT-B" });

        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.LineId);
        Assert.Equal("LOT-B", row.Lot);
    }

    // AC4: lọc theo Model — chỉ trả về dòng khớp đúng Model.
    [Fact]
    public async Task GetReportAsync_AC4_LocTheoModel_ChiTraVeDongKhopModel()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" },
            new() { Id = 20, LineId = 2, Model = "M2", Lot = "LOT-B" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery { Model = "M2" });

        var row = Assert.Single(result.Rows);
        Assert.Equal(2, row.LineId);
        Assert.Equal("M2", row.Model);
    }

    // AC4: nhiều Line khác nhau cùng đóng thùng song song -> tất cả cùng xuất hiện, không giới hạn theo 1 Line.
    [Fact]
    public async Task GetReportAsync_AC4_NhieuLineDongThoi_TraVeTatCaCacDong()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupLines(new List<Line> { new() { Id = 1, Name = "Line 1" }, new() { Id = 2, Name = "Line 2" } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
            new() { Id = 2, ProductionPlanId = 20, LineId = 2, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" },
            new() { Id = 20, LineId = 2, Model = "M2", Lot = "LOT-B" },
        });

        var result = await _sut.GetReportAsync(new PackingProgressReportQuery());

        Assert.Equal(2, result.Rows.Count);
    }

    // AC6: danh sách TẤT CẢ thùng (Completed lẫn InProgress) của 1 dòng báo cáo (Line + Lot) -> sắp xếp theo BoxNo tăng dần.
    [Fact]
    public async Task GetBoxesAsync_AC6_TraVeCaCompletedLanInProgress_SapXepTheoBoxNoTangDan()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan> { new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-A" } });
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 2, ProductionPlanId = 10, StageId = 100, BoxNo = 2, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 20), CompletedAtUtc = new DateTime(2026, 8, 20, 1, 0, 0) },
            new() { Id = 3, ProductionPlanId = 10, StageId = 100, BoxNo = 3, Status = PackingBoxStatus.InProgress, TargetQuantity = 20, ScannedQuantity = 5, OpenedAtUtc = new DateTime(2026, 8, 21) },
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20, OpenedAtUtc = new DateTime(2026, 8, 19) },
        });

        var result = await _sut.GetBoxesAsync(1, "LOT-A");

        Assert.Equal(3, result.Count);
        Assert.Equal(new[] { 1, 2, 3 }, result.Select(b => b.BoxNo).ToArray());
        Assert.Equal(PackingBoxStatus.InProgress, result[2].Status);
        Assert.Null(result[2].CompletedAtUtc);
    }

    // AC6: gộp theo Lot — kế hoạch cũ Cancelled + kế hoạch mới Running cùng Lot -> lấy thùng của cả 2, đúng cách gộp GetReportAsync đang dùng.
    [Fact]
    public async Task GetBoxesAsync_AC6_GopTheoLotGomCaKeHoachCuDaHuy_TraVeThungCuaCaHaiKeHoach()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });
        SetupPlanStages(new List<ProductionPlanStage>
        {
            new() { Id = 1, ProductionPlanId = 10, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Cancelled },
            new() { Id = 2, ProductionPlanId = 11, LineId = 1, StageId = 100, PlanStatus = PlanStatus.Running },
        });
        SetupPlans(new List<ProductionPlan>
        {
            new() { Id = 10, LineId = 1, Model = "M1", Lot = "LOT-X" },
            new() { Id = 11, LineId = 1, Model = "M1", Lot = "LOT-X" },
        });
        SetupPackingBoxes(new List<PackingBox>
        {
            new() { Id = 1, ProductionPlanId = 10, StageId = 100, BoxNo = 1, Status = PackingBoxStatus.Completed, TargetQuantity = 20, ScannedQuantity = 20 },
            new() { Id = 2, ProductionPlanId = 11, StageId = 100, BoxNo = 2, Status = PackingBoxStatus.InProgress, TargetQuantity = 20, ScannedQuantity = 5 },
        });

        var result = await _sut.GetBoxesAsync(1, "LOT-X");

        Assert.Equal(2, result.Count);
        Assert.Equal(new[] { 1, 2 }, result.Select(b => b.BoxNo).ToArray());
    }

    // AC6: Line/Lot không khớp bất kỳ ProductionPlanStage nào -> trả về danh sách rỗng, không lỗi.
    [Fact]
    public async Task GetBoxesAsync_AC6_KhongCoDuLieuKhopLineVaLot_TraVeRong()
    {
        SetupStages(new List<Stage> { new() { Id = 100, Name = "Đóng thùng", IsPackingStage = true } });

        var result = await _sut.GetBoxesAsync(99, "LOT-KHONG-TON-TAI");

        Assert.Empty(result);
    }

    // AC7: chỉ lấy Scan Ok gắn PackingBoxId đúng thùng, KHÔNG gồm lượt bị từ chối/Ng, sắp xếp theo thời điểm tăng dần.
    [Fact]
    public async Task GetBoxScansAsync_AC7_ChiLayScanOkGanDungThung_SapXepTheoThoiGianTangDan()
    {
        SetupPackingBoxById(new PackingBox { Id = 5, ScannedQuantity = 2 });
        SetupScans(new List<Scan>
        {
            new() { Id = 2, TagCode = "TAG-B", PackingBoxId = 5, Result = ScanResult.Ok, ScannedAtUtc = new DateTime(2026, 8, 25, 10, 0, 0) },
            new() { Id = 1, TagCode = "TAG-A", PackingBoxId = 5, Result = ScanResult.Ok, ScannedAtUtc = new DateTime(2026, 8, 25, 9, 0, 0) },
            // Không cùng thùng -> loại.
            new() { Id = 3, TagCode = "TAG-C", PackingBoxId = 6, Result = ScanResult.Ok, ScannedAtUtc = new DateTime(2026, 8, 25, 9, 30, 0) },
            // Bị từ chối tại đúng thùng (lý thuyết PackingBoxId chỉ set khi Ok, nhưng test phòng vệ) -> loại.
            new() { Id = 4, TagCode = "TAG-D", PackingBoxId = 5, Result = ScanResult.DuplicateTag, ScannedAtUtc = new DateTime(2026, 8, 25, 9, 45, 0) },
        });

        var result = await _sut.GetBoxScansAsync(5);

        Assert.True(result.HasDetailedScanData);
        Assert.Equal(2, result.Scans.Count);
        Assert.Equal(new[] { "TAG-A", "TAG-B" }, result.Scans.Select(s => s.TagCode).ToArray());
    }

    // AC8: thùng cũ (ScannedQuantity > 0 nhưng KHÔNG có Scan nào gắn PackingBoxId, ghi trước khi field tồn tại) -> HasDetailedScanData = false.
    [Fact]
    public async Task GetBoxScansAsync_AC8_ThungCuKhongCoDuLieuChiTiet_HasDetailedScanDataFalse()
    {
        SetupPackingBoxById(new PackingBox { Id = 7, ScannedQuantity = 20 });
        SetupScans(new List<Scan>());

        var result = await _sut.GetBoxScansAsync(7);

        Assert.False(result.HasDetailedScanData);
        Assert.Empty(result.Scans);
    }

    // AC8 (edge case): thùng THẬT SỰ chưa có scan nào (ScannedQuantity = 0, vd vừa mở) -> vẫn là "có dữ liệu chi tiết" (0 lượt thật), KHÔNG bị coi là thùng cũ.
    [Fact]
    public async Task GetBoxScansAsync_ThungThatSuChuaCoScanNao_HasDetailedScanDataTrue()
    {
        SetupPackingBoxById(new PackingBox { Id = 8, ScannedQuantity = 0 });
        SetupScans(new List<Scan>());

        var result = await _sut.GetBoxScansAsync(8);

        Assert.True(result.HasDetailedScanData);
        Assert.Empty(result.Scans);
    }

    // Thùng không tồn tại -> EntityNotFoundException.
    [Fact]
    public async Task GetBoxScansAsync_ThungKhongTonTai_NemEntityNotFoundException()
    {
        SetupPackingBoxById(null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetBoxScansAsync(999));
    }
}
