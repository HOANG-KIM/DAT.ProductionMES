using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Abstractions.Realtime;
using ProductionMES.Application.DTOs.Scans;
using ProductionMES.Application.Services.PackingBoxes;
using ProductionMES.Application.Services.ProductionPlanStages;
using ProductionMES.Application.Services.Scans;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="ScanService.ConfirmRejectedScanAsync"/> (US-27 AC5/AC6/AC10/AC12) — xác nhận LƯU 1
/// lượt scan bị hệ thống tự động từ chối, tách riêng khỏi <see cref="ScanServiceTests"/> (US-08, hành vi CHƯA lưu
/// của <see cref="ScanService.CreateAsync"/>) cho dễ đọc.
/// </summary>
public class ScanServiceRejectConfirmationTests
{
    private const int StageId = 100;

    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ScanService _sut;

    private const int SupervisorUserId = 10;
    private const string SupervisorUserName = "to_truong1";

    public ScanServiceRejectConfirmationTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Scan>());

        _sut = new ScanService(
            _unitOfWorkMock.Object,
            Mock.Of<IProductionPlanStageService>(),
            Mock.Of<IScanNotifier>(),
            Mock.Of<IPackingBoxService>());
    }

    private static ConfirmRejectedScanRequest BuildRequest(ScanResult result = ScanResult.DuplicateTag) => new()
    {
        TagCode = "TAG1",
        StageId = StageId,
        LineId = 1,
        WorkStationId = 1,
        ProductionPlanId = 1,
        Customer = "Khách hàng A",
        Model = "Model X",
        Lot = "LOT001",
        Revision = "B",
        PlannedQuantity = 500,
        TaktTimeSeconds = 25.5m,
        OperatorNames = "Nguyễn Văn A",
        ScannedAtUtc = new DateTime(2026, 8, 25, 10, 0, 0),
        Result = result,
        RejectionReason = "Trùng tem tại công đoạn này.",
    };

    // AC6 — Đăng nhập thành công, đủ quyền -> lưu đúng snapshot, đúng thời gian scan GỐC (không phải lúc xác nhận), kèm người xác nhận.
    [Fact]
    public async Task ConfirmRejectedScanAsync_HopLe_LuuDungSnapshotVaThoiGianScanGocKemNguoiXacNhan()
    {
        var request = BuildRequest();

        var result = await _sut.ConfirmRejectedScanAsync(request, SupervisorUserId, SupervisorUserName);

        Assert.Equal(ScanResult.DuplicateTag, result.Result);
        Assert.Equal("TAG1", result.TagCode);
        Assert.Equal(request.ScannedAtUtc, result.ScannedAtUtc);
        Assert.Equal(SupervisorUserId, result.ConfirmedByUserId);
        Assert.Equal(SupervisorUserName, result.ConfirmedByUserName);
        Assert.Equal("Khách hàng A", result.Customer);
        Assert.Equal("Model X", result.Model);
        Assert.Equal("LOT001", result.Lot);

        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s =>
                s.TagCode == "TAG1" && s.Result == ScanResult.DuplicateTag && s.ScannedAtUtc == request.ScannedAtUtc
                && s.ConfirmedByUserId == SupervisorUserId && s.ConfirmedByUserName == SupervisorUserName),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC10/AC12 — áp dụng đồng nhất cho mọi ScanResult từ chối tự động, kể cả PreviousStageNotPassed/WaitingReworkUnlock.
    [Theory]
    [InlineData(ScanResult.PreviousStageNotPassed)]
    [InlineData(ScanResult.WaitingReworkUnlock)]
    public async Task ConfirmRejectedScanAsync_CacScanResultTuChoiTuDongKhac_LuuThanhCong(ScanResult result)
    {
        var request = BuildRequest(result);

        var dto = await _sut.ConfirmRejectedScanAsync(request, SupervisorUserId, SupervisorUserName);

        Assert.Equal(result, dto.Result);
        _scanRepositoryMock.Verify(r => r.AddAsync(It.Is<Scan>(s => s.Result == result), It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC10 — Result = Ok không được xác nhận qua endpoint này (đã lưu ngay ở CreateAsync).
    [Fact]
    public async Task ConfirmRejectedScanAsync_ResultOk_NemBusinessRuleExceptionKhongLuu()
    {
        var request = BuildRequest(ScanResult.Ok);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ConfirmRejectedScanAsync(request, SupervisorUserId, SupervisorUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC2/AC10 — Result = Ng không đi qua endpoint này (dùng ScanNgController/CreateNgAsync riêng, US-18).
    [Fact]
    public async Task ConfirmRejectedScanAsync_ResultNg_NemBusinessRuleExceptionKhongLuu()
    {
        var request = BuildRequest(ScanResult.Ng);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ConfirmRejectedScanAsync(request, SupervisorUserId, SupervisorUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Thiếu thông tin người xác nhận (Controller lẽ ra luôn truyền đủ sau khi xác thực Bearer) -> từ chối, không lưu.
    [Theory]
    [InlineData(0, SupervisorUserName)]
    [InlineData(-1, SupervisorUserName)]
    [InlineData(SupervisorUserId, "")]
    [InlineData(SupervisorUserId, "   ")]
    public async Task ConfirmRejectedScanAsync_ThieuThongTinNguoiXacNhan_NemBusinessRuleException(int confirmedByUserId, string confirmedByUserName)
    {
        var request = BuildRequest();

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.ConfirmRejectedScanAsync(request, confirmedByUserId, confirmedByUserName));
        _scanRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Scan>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
