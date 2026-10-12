using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Services.Testers;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>Unit test cho TesterStationService, bám theo US-11 AC4/AC5/AC10 (công tắc "Đang sử dụng máy thông điện").</summary>
public class TesterStationServiceTests
{
    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<TesterModeChangeLog>> _changeLogRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly TesterStationService _sut;

    public TesterStationServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<TesterModeChangeLog>()).Returns(_changeLogRepositoryMock.Object);
        _sut = new TesterStationService(_unitOfWorkMock.Object);
    }

    private void SetupWorkStation(bool useArduino, bool testerEnabled = true)
        => _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = 1, Name = "Trạm Thông điện", UseArduino = useArduino, TesterEnabled = testerEnabled });

    // AC10 — mặc định BẬT khi trạm mới.
    [Fact]
    public void WorkStation_TaoMoi_CongTacMacDinhBat()
    {
        Assert.True(new WorkStation().TesterEnabled);
    }

    // AC10 — App đọc lại đúng trạng thái từ server.
    [Fact]
    public async Task GetStateAsync_TramCoMayCongTacTat_TraVeDungTrangThai()
    {
        SetupWorkStation(useArduino: true, testerEnabled: false);

        var state = await _sut.GetStateAsync(1);

        Assert.Equal(1, state.WorkStationId);
        Assert.True(state.UseArduino);
        Assert.False(state.TesterEnabled);
    }

    [Fact]
    public async Task GetStateAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetStateAsync(99));
    }

    // AC4 — đổi BẬT -> TẮT: lưu trạng thái + 1 bản ghi log đủ trạm/từ/đến/lý do/người duyệt/thời điểm.
    [Fact]
    public async Task SetEnabledAsync_TatCongTac_DoiTrangThaiVaGhiLogDayDu()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);
        TesterModeChangeLog? savedLog = null;
        _changeLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TesterModeChangeLog>(), It.IsAny<CancellationToken>()))
            .Callback((TesterModeChangeLog l, CancellationToken _) => savedLog = l)
            .Returns(Task.CompletedTask);

        var before = DateTime.Now;
        var state = await _sut.SetEnabledAsync(1, enabled: false, "Máy hỏng", approvedByUserId: 7, approvedByUserName: "tt01");

        Assert.False(state.TesterEnabled);
        Assert.NotNull(savedLog);
        Assert.Equal(1, savedLog!.WorkStationId);
        Assert.Equal(TesterMode.Tester, savedLog.FromMode);
        Assert.Equal(TesterMode.Bypass, savedLog.ToMode);
        Assert.Equal("Máy hỏng", savedLog.Reason);
        Assert.Equal(7, savedLog.ApprovedByUserId);
        Assert.Equal("tt01", savedLog.ApprovedByUserName);
        Assert.True(savedLog.ChangedAtUtc >= before);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC8 — TẮT -> BẬT.
    [Fact]
    public async Task SetEnabledAsync_BatLaiCongTac_GhiLogBypassSangTester()
    {
        SetupWorkStation(useArduino: true, testerEnabled: false);
        TesterModeChangeLog? savedLog = null;
        _changeLogRepositoryMock.Setup(r => r.AddAsync(It.IsAny<TesterModeChangeLog>(), It.IsAny<CancellationToken>()))
            .Callback((TesterModeChangeLog l, CancellationToken _) => savedLog = l)
            .Returns(Task.CompletedTask);

        var state = await _sut.SetEnabledAsync(1, enabled: true, "Máy đã sửa xong", 7, "tt01");

        Assert.True(state.TesterEnabled);
        Assert.Equal(TesterMode.Bypass, savedLog!.FromMode);
        Assert.Equal(TesterMode.Tester, savedLog.ToMode);
    }

    // AC5 — thiếu lý do: không đổi, không ghi log.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetEnabledAsync_ThieuLyDo_NemBusinessRuleExceptionKhongGhiGi(string reason)
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetEnabledAsync(1, false, reason, 7, "tt01"));

        _changeLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TesterModeChangeLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC1/AC3 — trạm không có máy thì không có công tắc để đổi.
    [Fact]
    public async Task SetEnabledAsync_TramKhongCoMay_NemBusinessRuleException()
    {
        SetupWorkStation(useArduino: false);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetEnabledAsync(1, false, "Lý do", 7, "tt01"));

        _changeLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TesterModeChangeLog>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetEnabledAsync_ThieuNguoiDuyet_NemBusinessRuleException()
    {
        SetupWorkStation(useArduino: true);

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.SetEnabledAsync(1, false, "Lý do", 0, ""));
    }

    // Idempotent: đã ở đúng trạng thái -> không ghi log thừa.
    [Fact]
    public async Task SetEnabledAsync_DaODungTrangThai_KhongGhiLog()
    {
        SetupWorkStation(useArduino: true, testerEnabled: true);

        var state = await _sut.SetEnabledAsync(1, enabled: true, "Lý do", 7, "tt01");

        Assert.True(state.TesterEnabled);
        _changeLogRepositoryMock.Verify(r => r.AddAsync(It.IsAny<TesterModeChangeLog>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
