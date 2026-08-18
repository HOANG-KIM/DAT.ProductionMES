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
/// Unit test cho <see cref="ScanService.CreateNgAsync"/>/<see cref="ScanService.GetNgReasonSuggestionsAsync"/>
/// (US-18 AC3/AC4/AC5) — tách riêng khỏi <see cref="ScanServiceTests"/> (luồng OK/US-08) cho dễ đọc.
/// </summary>
public class ScanServiceNgTests
{
    private const int LapRapStageId = 100; // "Lắp ráp"
    private const int ThongDienStageId = 200; // "Thông điện", liền sau Lắp ráp

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

    public ScanServiceNgTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlan>()).Returns(_productionPlanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ProductionPlanStage>()).Returns(_productionPlanStageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Stage>()).Returns(_stageRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        _sut = new ScanService(_unitOfWorkMock.Object, _productionPlanStageServiceMock.Object, _scanNotifierMock.Object);

        SetupExistingScans(new List<Scan>());

        // US-19: mặc định không có ReworkUnlock nào — test CreateAsync (vd SauKhiNg_TemBiKhoaKhiScanSangCongDoanKeTiep) cần repo này không null.
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ReworkUnlock>());

        _productionPlanStageRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ProductionPlanStage, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStage>());
    }

    private void SetupExistingScans(List<Scan> existingScans)
    {
        _existingScans = existingScans;

        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                _existingScans.Where(predicate.Compile()).ToList());

        _scanRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()))
            .Callback((Scan scan, CancellationToken _) => _existingScans.Add(scan))
            .Returns(Task.CompletedTask);
    }

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

    // US-18 (thay đổi 18/08/2026): tài khoản Tổ trưởng mẫu dùng chung cho các test dưới đây (Id/Username từ claim Bearer, KHÔNG phải WorkStationId).
    private const int SupervisorUserId = 10;
    private const string SupervisorUserName = "to_truong1";

    // AC3 — bắt buộc nhập lý do trước khi ghi nhận.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateNgAsync_LyDoRong_NemBusinessRuleException(string reason)
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateNgAsync(1, "TAG1", reason, SupervisorUserId, SupervisorUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // US-18 (thay đổi 18/08/2026): thiếu thông tin người xác nhận (Controller lẽ ra luôn truyền đủ sau khi xác thực Bearer) -> từ chối, không lưu Scan.
    [Theory]
    [InlineData(0, SupervisorUserName)]
    [InlineData(-1, SupervisorUserName)]
    [InlineData(SupervisorUserId, "")]
    [InlineData(SupervisorUserId, "   ")]
    public async Task CreateNgAsync_ThieuThongTinNguoiXacNhan_NemBusinessRuleException(int confirmedByUserId, string confirmedByUserName)
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateNgAsync(1, "TAG1", "Lỗi hàn", confirmedByUserId, confirmedByUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC3/AC5 (thay đổi 18/08/2026) — Scan NG hợp lệ: ghi Result = Ng kèm lý do, đủ mã tem/công đoạn/trạm/thời gian/người xác nhận (tài khoản đăng nhập ở AC2a).
    [Fact]
    public async Task CreateNgAsync_HopLe_GhiNhanNgKemLyDoVaNguoiXacNhanDayDu()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);

        var result = await _sut.CreateNgAsync(1, "TAG1", "  Trầy xước vỏ  ", SupervisorUserId, SupervisorUserName);

        Assert.Equal(ScanResult.Ng, result.Result);
        Assert.Equal("Trầy xước vỏ", result.RejectionReason); // Trim() lý do
        Assert.Equal("TAG1", result.TagCode);
        Assert.Equal(LapRapStageId, result.StageId);
        Assert.Equal(1, result.WorkStationId);
        Assert.Equal(1, result.ProductionPlanId);
        Assert.Equal(SupervisorUserId, result.ConfirmedByUserId);
        Assert.Equal(SupervisorUserName, result.ConfirmedByUserName);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s =>
                s.TagCode == "TAG1" && s.Result == ScanResult.Ng && s.RejectionReason == "Trầy xước vỏ" && s.WorkStationId == 1
                && s.ConfirmedByUserId == SupervisorUserId && s.ConfirmedByUserName == SupervisorUserName),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC3 — Scan NG KHÔNG chạy kiểm tra chống trùng tem: tem đã có 1 bản ghi Ok tại đúng công đoạn này vẫn quét NG được (vd rework nhiều lần, US-19).
    [Fact]
    public async Task CreateNgAsync_TemDaCoBanGhiOkTaiCungCongDoan_VanGhiNhanNgBinhThuong()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = LapRapStageId, LineId = 1, Result = ScanResult.Ok } });

        var result = await _sut.CreateNgAsync(1, "TAG1", "Lỗi hàn", SupervisorUserId, SupervisorUserName);

        Assert.Equal(ScanResult.Ng, result.Result);
    }

    // AC3 (hệ quả tự nhiên) — Tem chỉ có bản ghi Ng tại 1 công đoạn (chưa có Ok) bị khóa khi cố scan OK sang công đoạn kế tiếp.
    [Fact]
    public async Task SauKhiNg_TemBiKhoaKhiScanSangCongDoanKeTiep()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = LapRapStageId, Name = "Trạm Lắp ráp Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: LapRapStageId, productionPlanId: 1);

        var ngResult = await _sut.CreateNgAsync(1, "TAG1", "Trầy xước vỏ", SupervisorUserId, SupervisorUserName);
        Assert.Equal(ScanResult.Ng, ngResult.Result);

        // Trạm công đoạn kế tiếp (Thông điện), liền sau Lắp ráp.
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 2, LineId = 1, StageId = ThongDienStageId, Name = "Trạm Thông điện Line 1" });
        SetupRunningPlanStage(lineId: 1, stageId: ThongDienStageId, productionPlanId: 1);
        _productionPlanStageServiceMock.Setup(s => s.GetByProductionPlanAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductionPlanStageDto>
            {
                new() { Id = 1, ProductionPlanId = 1, StageId = LapRapStageId, SequenceNumber = 1, PreviousStageId = null },
                new() { Id = 2, ProductionPlanId = 1, StageId = ThongDienStageId, SequenceNumber = 2, PreviousStageId = LapRapStageId },
            });

        var okAttempt = await _sut.CreateAsync(2, "TAG1");

        Assert.Equal(ScanResult.PreviousStageNotPassed, okAttempt.Result);
    }

    // Không có kế hoạch Running -> lỗi rõ ràng, không lưu Scan (giống CreateAsync).
    [Fact]
    public async Task CreateNgAsync_KhongCoKeHoachNaoRunning_NemBusinessRuleException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 5, LineId = 9, StageId = LapRapStageId, Name = "Trạm chưa có kế hoạch" });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.CreateNgAsync(5, "TAG1", "Lỗi bất kỳ", SupervisorUserId, SupervisorUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateNgAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.CreateNgAsync(999, "TAG1", "Lỗi bất kỳ", SupervisorUserId, SupervisorUserName));
    }

    // AC4 — gợi ý lý do: không trùng lặp, sắp theo lần dùng gần nhất trước, chỉ lấy đúng công đoạn được hỏi.
    [Fact]
    public async Task GetNgReasonSuggestionsAsync_TraVeKhongTrungLapSapTheoLanDungGanNhat()
    {
        var now = DateTime.UtcNow;
        SetupExistingScans(new List<Scan>
        {
            new() { StageId = LapRapStageId, Result = ScanResult.Ng, RejectionReason = "Trầy xước vỏ", ScannedAtUtc = now.AddHours(-3) },
            new() { StageId = LapRapStageId, Result = ScanResult.Ng, RejectionReason = "Lỗi hàn", ScannedAtUtc = now.AddHours(-2) },
            // Cùng lý do "Trầy xước vỏ" nhưng dùng gần đây hơn -> phải đẩy lên đầu danh sách (không trùng lặp).
            new() { StageId = LapRapStageId, Result = ScanResult.Ng, RejectionReason = "Trầy xước vỏ", ScannedAtUtc = now.AddHours(-1) },
            // Khác công đoạn -> không được tính vào.
            new() { StageId = ThongDienStageId, Result = ScanResult.Ng, RejectionReason = "Lỗi thông điện", ScannedAtUtc = now },
            // Không phải Ng -> không được tính vào.
            new() { StageId = LapRapStageId, Result = ScanResult.Ok, RejectionReason = null, ScannedAtUtc = now },
        });

        var suggestions = await _sut.GetNgReasonSuggestionsAsync(LapRapStageId);

        Assert.Equal(new[] { "Trầy xước vỏ", "Lỗi hàn" }, suggestions);
    }

    [Fact]
    public async Task GetNgReasonSuggestionsAsync_ChuaCoLyDoNao_TraVeDanhSachRong()
    {
        var suggestions = await _sut.GetNgReasonSuggestionsAsync(LapRapStageId);

        Assert.Empty(suggestions);
    }
}
