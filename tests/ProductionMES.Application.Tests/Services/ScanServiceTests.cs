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
/// Unit test cho ScanService, bám theo AC-01 → AC-04 (mục 7 SRS), AC1-AC5 của US-08, và AC5 của US-05a (tự
/// động Completed) (Documents/BACKLOG-user-story.md).
/// </summary>
public class ScanServiceTests
{
    private const int LapRapStageId = 100; // "Lắp ráp"
    private const int ThongDienStageId = 200; // "Thông điện"

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

    public ScanServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object);

        _stageRepositoryMock.Setup(r => r.GetByIdAsync(LapRapStageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = LapRapStageId, Name = "Lắp ráp", IsActive = true });
        _stageRepositoryMock.Setup(r => r.GetByIdAsync(ThongDienStageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Stage { Id = ThongDienStageId, Name = "Thông điện", IsActive = true });

        // Mô phỏng repository thật: FindAsync lọc trên "dữ liệu" hiện có trong _scanRepositoryMock (mặc định rỗng,
        // từng test override qua SetupExistingScans). AddAsync đẩy bản ghi mới vào cùng danh sách để các lượt
        // FindAsync SAU đó (vd tính RunCount cho US-05a AC5) thấy được bản ghi vừa lưu.
        SetupExistingScans(new List<Scan>());

        // US-19: mặc định không có ReworkUnlock nào — từng test override qua SetupExistingReworkUnlocks.
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReworkUnlock>());

        // Mặc định không có ProductionPlanStage nào Running — từng test override qua SetupRunningPlanStage.
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());

        // US-21 AC8: GetHistoryAsync tra thêm IRepository<WorkStation>.FindAsync để lấy tên trạm — mặc định rỗng
        // (WorkStationName trả về chuỗi rỗng), từng test AC8 override qua SetupExistingWorkStations nếu cần.
        _workStationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<WorkStation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WorkStation>());
    }

    /// <summary>US-21 AC8: mô phỏng bảng WorkStation hiện có trong DB, dùng cho GetHistoryAsync.</summary>
    private void SetupExistingWorkStations(List<WorkStation> existingWorkStations)
    {
        _workStationRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<WorkStation, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<WorkStation, bool>> predicate, CancellationToken _) =>
                existingWorkStations.Where(predicate.Compile()).ToList());
    }

    /// <summary>US-19: mô phỏng bảng ReworkUnlock hiện có trong DB.</summary>
    private void SetupExistingReworkUnlocks(List<ReworkUnlock> existingUnlocks)
    {
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ReworkUnlock, bool>> predicate, CancellationToken _) =>
                existingUnlocks.Where(predicate.Compile()).ToList());
    }

    /// <summary>Mô phỏng bảng Scan hiện có trong DB — FindAsync sẽ áp đúng predicate của ScanService lên danh sách này.</summary>
    private void SetupExistingScans(List<Scan> existingScans)
    {
        _existingScans = existingScans;

        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                _existingScans.Where(predicate.Compile()).ToList());

        // Mô phỏng AddAsync ghi thật vào "DB" cục bộ này, để các FindAsync gọi SAU (vd tính RunCount cho US-05a
        // AC5, thực hiện SAU khi lượt scan OK vừa được lưu) thấy đúng bản ghi vừa thêm.
        _scanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()))
            .Callback((Scan scan, CancellationToken _) => _existingScans.Add(scan))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Cấu hình 1 ProductionPlanStage đang Running cho (lineId, stageId), thuộc kế hoạch productionPlanId — mô
    /// phỏng "kế hoạch active của trạm" theo model mới (US-05a). plannedQuantity mặc định rất lớn để không vô
    /// tình kích hoạt auto-Completed (US-05a AC5) ở các test không liên quan.
    /// </summary>
    private void SetupRunningPlanStage(int lineId, int stageId, int productionPlanId, int plannedQuantity = 1_000_000)
    {
        var runningPlanStage = new ProductionPlanStage
        {
            Id = productionPlanId * 1000 + stageId,
            ProductionPlanId = productionPlanId,
            StageId = stageId,
            LineId = lineId,
            PlanStatus = PlanStatus.Running,
        };

        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { runningPlanStage }.Where(predicate.Compile()).ToList());

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(productionPlanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan { Id = productionPlanId, LineId = lineId, PlannedQuantity = plannedQuantity });
    }

    /// <summary>Cấu hình trạm Line2/Thông điện, kế hoạch Running Id=2, công đoạn "Thông điện" liền sau "Lắp ráp" (Sequence 2, Previous = LapRapStageId).</summary>
    private void SetupLine2ThongDienVoiCongDoanLienTruocLaLapRap()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 2, LineId = 2, StageId = ThongDienStageId, Name = "Trạm Thông điện Line 2" });

        SetupRunningPlanStage(lineId: 2, stageId: ThongDienStageId, productionPlanId: 2);

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
        SetupRunningPlanStage(lineId: 2, stageId: LapRapStageId, productionPlanId: 2);
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
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        var result = await _sut.CreateAsync(1, "C");

        Assert.Equal(ScanResult.Ok, result.Result);
        // Không truy vấn kiểm tra công đoạn liền trước vì PreviousStageId = null -> chỉ 1 lần FindAsync (bước chống trùng tem)
        // + 1 lần FindAsync tính RunCount cho US-05a AC5 = 2 lần.
        _scanRepositoryMock.Verify(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    // Không có kế hoạch nào Running cho (Line, Công đoạn) của trạm -> lỗi rõ ràng (BusinessRuleException), không NullReference.
    [Fact]
    public async Task CreateAsync_KhongCoKeHoachNaoRunningChoLineCongDoanCuaTram_NemBusinessRuleException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 5, LineId = 9, StageId = LapRapStageId, Name = "Trạm chưa có kế hoạch" });
        // Mặc định (constructor) productionPlanStageRepository trả về rỗng -> không có Running nào.

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

    // Dữ liệu bất nhất (phòng vệ): có ProductionPlanStage Running cho (Line, Công đoạn) của trạm, nhưng
    // GetByProductionPlanAsync (mock riêng) lại không trả về đúng công đoạn đó -> lỗi rõ ràng, không lưu Scan.
    [Fact]
    public async Task CreateAsync_DuLieuBatNhatKhongTimThayCauHinhCongDoan_NemBusinessRuleException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 6, LineId = 1, StageId = ThongDienStageId, Name = "Trạm chưa cấu hình" });
        SetupRunningPlanStage(lineId: 1, stageId: ThongDienStageId, productionPlanId: 1);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateAsync(6, "F"));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-05a AC5 — Lượt scan OK làm đủ số lượng kế hoạch -> tự động chuyển ProductionPlanStage sang Completed.
    [Fact]
    public async Task CreateAsync_ScanOkLamDuSoLuongKeHoach_TuDongChuyenCompleted()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        // PlannedQuantity = 5, đã có sẵn 4 lượt scan OK -> lượt scan thứ 5 này sẽ đủ số lượng.
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1, plannedQuantity: 5);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });
        SetupExistingScans(Enumerable.Range(1, 4)
            .Select(i => new Scan { TagCode = $"TAG{i}", StageId = LapRapStageId, LineId = 1, ProductionPlanId = 1, Result = ScanResult.Ok })
            .ToList());

        var result = await _sut.CreateAsync(1, "TAG5");

        Assert.Equal(ScanResult.Ok, result.Result);
        _productionPlanStageRepositoryMock.Verify(
            r => r.Update(It.Is<ProductionPlanStage>(x => x.ProductionPlanId == 1 && x.StageId == LapRapStageId && x.PlanStatus == PlanStatus.Completed)),
            Times.Once);
    }

    // US-10 AC1 — Lượt scan lưu đúng 6 field snapshot (Customer/Model/Lot/Revision/PlannedQuantity/TaktTimeSeconds)
    // từ ProductionPlan tại thời điểm scan. US-10 AC1/AC5 (bổ sung 19/08/2026): cũng snapshot đúng OperatorNames.
    [Fact]
    public async Task CreateAsync_ScanThanhCong_LuuDungSnapshot6FieldTuProductionPlan()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });

        var runningPlanStage = new ProductionPlanStage
        {
            Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, LineId = 1, PlanStatus = PlanStatus.Running,
        };
        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ProductionPlanStage, bool>> predicate, CancellationToken _) =>
                new List<ProductionPlanStage> { runningPlanStage }.Where(predicate.Compile()).ToList());

        _productionPlanRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductionPlan
            {
                Id = 1,
                LineId = 1,
                Customer = "Khách hàng A",
                Model = "Model X",
                Lot = "LOT001",
                Revision = "B",
                PlannedQuantity = 500,
                TaktTimeSeconds = 25.5m,
                OperatorNames = "Nguyễn Văn A, Trần Thị B",
            });

        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        var result = await _sut.CreateAsync(1, "SNAP1");

        Assert.Equal(ScanResult.Ok, result.Result);
        Assert.Equal("Khách hàng A", result.Customer);
        Assert.Equal("Model X", result.Model);
        Assert.Equal("LOT001", result.Lot);
        Assert.Equal("B", result.Revision);
        Assert.Equal(500, result.PlannedQuantity);
        Assert.Equal(25.5m, result.TaktTimeSeconds);
        Assert.Equal("Nguyễn Văn A, Trần Thị B", result.OperatorNames);

        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s =>
                s.Customer == "Khách hàng A" &&
                s.Model == "Model X" &&
                s.Lot == "LOT001" &&
                s.Revision == "B" &&
                s.PlannedQuantity == 500 &&
                s.TaktTimeSeconds == 25.5m &&
                s.OperatorNames == "Nguyễn Văn A, Trần Thị B"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // US-05a AC5 (mặt trái) — Scan OK nhưng CHƯA đủ số lượng -> KHÔNG chuyển Completed.
    [Fact]
    public async Task CreateAsync_ScanOkChuaDuSoLuongKeHoach_KhongChuyenCompleted()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1, plannedQuantity: 100);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
            });

        var result = await _sut.CreateAsync(1, "TAG1");

        Assert.Equal(ScanResult.Ok, result.Result);
        _productionPlanStageRepositoryMock.Verify(r => r.Update(It.IsAny<ProductionPlanStage>()), Times.Never);
    }

    // Mở rộng 18/08/2026 (US-21 AC7): GetHistoryAsync thêm filter Lot/Model/Customer/Revision/StageId, kết hợp
    // AND với các filter cũ (US-10 AC3) — phục vụ drill-down từ báo cáo tổng hợp (US-21).
    [Fact]
    public async Task GetHistoryAsync_LocTheoLot_ChiTraVeDungLotDo()
    {
        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "A", Lot = "LOT1", StageId = LapRapStageId, LineId = 1, ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok },
            new() { Id = 2, TagCode = "B", Lot = "LOT2", StageId = LapRapStageId, LineId = 1, ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok },
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1" });

        var item = Assert.Single(result.Items);
        Assert.Equal("LOT1", item.Lot);
    }

    [Fact]
    public async Task GetHistoryAsync_LocTheoStageIdModelCustomerRevision_KetHopAND()
    {
        SetupExistingScans(new List<Scan>
        {
            new()
            {
                Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, Model = "M1", Customer = "C1",
                Revision = "R1", ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok,
            },
            // Khác Model -> không khớp dù cùng StageId/Customer/Revision.
            new()
            {
                Id = 2, TagCode = "B", StageId = LapRapStageId, LineId = 1, Model = "M2", Customer = "C1",
                Revision = "R1", ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok,
            },
            // Khác StageId -> không khớp dù các field còn lại giống hệt.
            new()
            {
                Id = 3, TagCode = "C", StageId = ThongDienStageId, LineId = 1, Model = "M1", Customer = "C1",
                Revision = "R1", ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok,
            },
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery
        {
            StageId = LapRapStageId,
            Model = "M1",
            Customer = "C1",
            Revision = "R1",
        });

        var item = Assert.Single(result.Items);
        Assert.Equal(1, item.Id);
    }

    [Fact]
    public async Task GetHistoryAsync_HienThiDayDuLyDoNgVaNguoiXacNhan_PhucVuDrillDownAC8()
    {
        SetupExistingScans(new List<Scan>
        {
            new()
            {
                Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, Lot = "LOT1",
                Result = ScanResult.Ng, RejectionReason = "Trầy xước", ConfirmedByUserId = 7,
                ConfirmedByUserName = "to.truong", ScannedAtUtc = DateTime.UtcNow,
            },
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1" });

        var item = Assert.Single(result.Items);
        Assert.Equal(ScanResult.Ng, item.Result);
        Assert.Equal("Trầy xước", item.RejectionReason);
        Assert.Equal(7, item.ConfirmedByUserId);
        Assert.Equal("to.truong", item.ConfirmedByUserName);
    }

    // US-21 AC8: hiển thị đúng tên trạm làm việc thực hiện lượt scan (không phải tên Operator).
    [Fact]
    public async Task GetHistoryAsync_TraVeDungTenTramLamViec_PhucVuDrillDownAC8()
    {
        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, WorkStationId = 5, Lot = "LOT1", ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok },
        });
        SetupExistingWorkStations(new List<WorkStation> { new() { Id = 5, Name = "Trạm Lắp ráp 01", LineId = 1, StageId = LapRapStageId } });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1" });

        var item = Assert.Single(result.Items);
        Assert.Equal(5, item.WorkStationId);
        Assert.Equal("Trạm Lắp ráp 01", item.WorkStationName);
    }

    // US-21 AC10/AC11: lượt Ok không có trạng thái rework (null) — chỉ có ý nghĩa với lượt Ng.
    [Fact]
    public async Task GetHistoryAsync_LuotOk_KhongCoTrangThaiRework()
    {
        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, Lot = "LOT1", ScannedAtUtc = DateTime.UtcNow, Result = ScanResult.Ok },
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1" });

        var item = Assert.Single(result.Items);
        Assert.Null(item.ReworkStatus);
        Assert.Null(item.ReworkUnlockedByUserName);
    }

    // US-21 AC10/AC11: lượt Ng đã được mở khóa (ReworkUnlock) và chưa scan lại -> "WaitingRescan" kèm đúng người mở khóa.
    [Fact]
    public async Task GetHistoryAsync_LuotNgDaMoKhoaChuaScanLai_TraVeTrangThaiChoScanLaiKemNguoiSuaHang()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddMinutes(5);

        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, Lot = "LOT1", ScannedAtUtc = ngAt, Result = ScanResult.Ng },
        });
        SetupExistingReworkUnlocks(new List<ReworkUnlock>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, UnlockedAtUtc = unlockAt, UnlockedByUserId = 9, UnlockedByUserName = "to_truong_b", Note = "Đã thay linh kiện." },
        });

        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1" });

        var item = Assert.Single(result.Items);
        Assert.Equal(ReworkStatus.WaitingRescan, item.ReworkStatus);
        Assert.Equal("to_truong_b", item.ReworkUnlockedByUserName);
        Assert.Equal(unlockAt, item.ReworkUnlockedAtUtc);
        Assert.Equal("Đã thay linh kiện.", item.ReworkUnlockNote);
        Assert.Null(item.ReworkStillNgOccurrence);
    }

    // US-21 AC10: trạng thái rework phải suy luận trên TOÀN BỘ lịch sử, không bị giới hạn bởi filter From/To của
    // chính lượt tra cứu — lượt mở khóa/scan lại nằm NGOÀI khoảng đang xem vẫn phải được tính đúng.
    [Fact]
    public async Task GetHistoryAsync_ReworkUnlockNamNgoaiKhoangThoiGianDangLoc_VanTinhDungTrangThai()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddDays(1); // Nằm ngoài khoảng [ngAt, ngAt+1h] đang lọc bên dưới.
        var okAt = unlockAt.AddMinutes(5);

        SetupExistingScans(new List<Scan>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, LineId = 1, Lot = "LOT1", ScannedAtUtc = ngAt, Result = ScanResult.Ng },
            new() { Id = 2, TagCode = "A", StageId = LapRapStageId, LineId = 1, Lot = "LOT1", ScannedAtUtc = okAt, Result = ScanResult.Ok },
        });
        SetupExistingReworkUnlocks(new List<ReworkUnlock>
        {
            new() { Id = 1, TagCode = "A", StageId = LapRapStageId, UnlockedAtUtc = unlockAt, UnlockedByUserId = 9, UnlockedByUserName = "to_truong_b" },
        });

        // Chỉ lọc đúng khoảng chứa lượt Ng (không chứa unlock/rescan) -> kết quả trả về đúng 1 lượt Ng, nhưng
        // trạng thái rework của nó phải phản ánh đúng "đã sửa xong" dựa trên lịch sử ĐẦY ĐỦ.
        var result = await _sut.GetHistoryAsync(new ScanHistoryQuery { Lot = "LOT1", FromUtc = ngAt, ToUtc = ngAt.AddHours(1) });

        var item = Assert.Single(result.Items);
        Assert.Equal(ScanResult.Ng, item.Result);
        Assert.Equal(ReworkStatus.Fixed, item.ReworkStatus);
        Assert.Equal("to_truong_b", item.ReworkUnlockedByUserName);
    }
}
