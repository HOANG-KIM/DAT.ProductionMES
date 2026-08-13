using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.ProductionPlanStages;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho ScanService, bám theo AC-01 → AC-04 (mục 7 SRS) và AC1-AC5 của US-08 (Documents/BACKLOG-user-story.md).
/// </summary>
public class ScanServiceTests
{
    private const int LapRapStageId = 100; // "Lắp ráp"
    private const int ThongDienStageId = 200; // "Thông điện"

    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<ProductionPlan>> _productionPlanRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<Stage>> _stageRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProductionPlanStageService> _productionPlanStageServiceMock = new();
    private readonly Mock<IScanNotifier> _scanNotifierMock = new();
    private readonly ScanService _sut;

    public ScanServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object);

        _stageRepositoryMock.Setup(r => r.GetByIdAsync(LapRapStageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = LapRapStageId, Name = "Lắp ráp", IsActive = true });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(ThongDienStageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = ThongDienStageId, Name = "Thông điện", IsActive = true });

        // Mô phỏng repository thật: FindAsync lọc trên "dữ liệu" hiện có trong _scanRepositoryMock (mặc định rỗng,
        // từng test override qua SetupExistingScans).
        SetupExistingScans(new List<Scan>());
    }

    /// <summary>Mô phỏng bảng Scan hiện có trong DB — FindAsync sẽ áp đúng predicate của ScanService lên danh sách này.</summary>
    private void SetupExistingScans(List<Scan> existingScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                existingScans.Where(predicate.Compile()).ToList());
    }

    /// <summary>Cấu hình trạm Line2/Thông điện, kế hoạch active Id=2, công đoạn "Thông điện" liền sau "Lắp ráp" (Sequence 2, Previous = LapRapStageId).</summary>
    private void SetupLine2ThongDienVoiCongDoanLienTruocLaLapRap()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 2, LineId = 2, StageId = ThongDienStageId, Name = "Trạm Thông điện Line 2" });

        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan> { new() { Id = 2, LineId = 2, IsActive = true } });

        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 2, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
                new() { Id = 2, ProductionPlanId = 2, StageId = ThongDienStageId, SequenceNumber = 2, PreviousStageId = LapRapStageId },
            });
    }

    // AC-01 SRS / US-08 AC1 — Trùng tem cùng công đoạn khác Line: tem A đã OK "Lắp ráp" Line 1, scan lại "Lắp ráp" Line 2 -> từ chối.
    [Fact]
    public async Task CreateAsync_TrungTemCungCongDoanKhacLine_TuChoiDuplicateTag()
    {
        // Trạm Line 2, công đoạn Lắp ráp (StageId trùng với công đoạn tem A đã OK ở Line 1) — công đoạn đầu tiên (không có liền trước).
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 3, LineId = 2, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 2" });
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan> { new() { Id = 2, LineId = 2, IsActive = true } });
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 2, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        // Tem A đã có 1 bản ghi OK tại đúng (TagCode="A", StageId=LapRapStageId), ghi nhận trước đó ở Line 1 — không quan
        // trọng LineId của bản ghi cũ vì rule chống trùng tra cứu toàn hệ thống theo StageId.
        SetupExistingScans(new List<Scan> { new() { TagCode = "A", StageId = LapRapStageId, LineId = 1, Result = ScanResult.Ok } });

        var result = await _sut.CreateAsync(3, "A");

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.Equal("Trùng tem tại công đoạn này.", result.RejectionReason);
        // FR-10: lượt scan bị từ chối vẫn được lưu.
        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s => s.Result == ScanResult.DuplicateTag && s.TagCode == "A"), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        // Không bắn sự kiện real-time cho lượt scan bị từ chối.
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC-02 SRS / US-08 AC2 — Khác công đoạn, khác Line hợp lệ: tem A đã OK "Lắp ráp" Line 1, scan "Thông điện" Line 2 -> chấp nhận.
    [Fact]
    public async Task CreateAsync_CungTemKhacCongDoanKhacLine_ChapNhanOk()
    {
        SetupLine2ThongDienVoiCongDoanLienTruocLaLapRap();

        // Tem A: chưa từng scan tại "Thông điện", đã OK tại "Lắp ráp" (công đoạn liền trước) ở Line 1 -> qua đủ 2 bước.
        SetupExistingScans(new List<Scan> { new() { TagCode = "A", StageId = LapRapStageId, LineId = 1, Result = ScanResult.Ok } });

        var result = await _sut.CreateAsync(2, "A");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Null(result.RejectionReason);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(2, It.Is<ScanResultDto>(d => d.Result == ScanResult.Ok), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC-03 SRS / US-08 AC3 — Chưa qua công đoạn liền trước: tem B chưa từng "Lắp ráp" -> từ chối, nêu rõ tên công đoạn thiếu.
    [Fact]
    public async Task CreateAsync_ChuaQuaCongDoanLienTruoc_TuChoiVaNeuRoTenCongDoanThieu()
    {
        SetupLine2ThongDienVoiCongDoanLienTruocLaLapRap();

        // Không có bản ghi Scan nào cho tem B (dù ở công đoạn Thông điện hay Lắp ráp) — dùng default rỗng từ constructor.

        var result = await _sut.CreateAsync(2, "B");

        Assert.Equal(ScanResult.PreviousStageNotPassed, result.Result);
        Assert.Equal("Chưa qua công đoạn: Lắp ráp", result.RejectionReason);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s => s.Result == ScanResult.PreviousStageNotPassed), It.IsAny<CancellationToken>()), Times.Once);
        _scanNotifierMock.Verify(n => n.NotifyScanRecordedAsync(It.IsAny<int>(), It.IsAny<ScanResultDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-08 AC4 — Qua đủ 2 bước kiểm tra -> ghi nhận OK (công đoạn đầu tiên, không có công đoạn liền trước).
    [Fact]
    public async Task CreateAsync_CongDoanDauTienChuaTrung_GhiNhanOk()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan> { new() { Id = 1, LineId = 1, IsActive = true } });
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        var result = await _sut.CreateAsync(1, "C");

        Assert.Equal(ScanResult.Ok, result.Result);
        // Không truy vấn kiểm tra công đoạn liền trước vì PreviousStageId = null -> chỉ 1 lần FindAsync (bước chống trùng tem).
        _scanRepositoryMock.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    // Không có kế hoạch active trên Line của trạm -> lỗi rõ ràng (BusinessRuleException), không NullReference.
    [Fact]
    public async Task CreateAsync_LineKhongCoKeHoachActive_NemBusinessRuleException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 5, LineId = 9, StageId = LapRapStageId, Name = "Trạm chưa có kế hoạch" });
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan>());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(5, "D"));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Trạm không tồn tại (phòng vệ) -> EntityNotFoundException, không NullReference.
    [Fact]
    public async Task CreateAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateAsync(999, "E"));
    }

    // Công đoạn của trạm chưa được cấu hình trong kế hoạch active -> lỗi rõ ràng (BusinessRuleException), không lưu Scan.
    [Fact]
    public async Task CreateAsync_CongDoanTramChuaCauHinhTrongKeHoachActive_NemBusinessRuleException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 6, LineId = 1, StageId = ThongDienStageId, Name = "Trạm chưa cấu hình" });
        _productionPlanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlan> { new() { Id = 1, LineId = 1, IsActive = true } });
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(6, "F"));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
