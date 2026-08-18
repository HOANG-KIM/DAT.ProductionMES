using System.Linq.Expressions;
using Moq;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Application.Services.ReworkUnlocks;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;
using ProductionMES.Domain.Exceptions;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test cho <see cref="ReworkUnlockService"/> (US-19 AC2/AC6, FR-19). AC6 (chặn quyền Công nhân) được thực
/// thi ở tầng Controller/policy (ADR-004, <c>[Authorize(Policy = Scan.ReworkUnlock)]</c>) — không lặp lại ở đây,
/// chỉ test hành vi Service khi ĐÃ có đủ (UnlockedByUserId, UnlockedByUserName) hợp lệ, và hành vi phòng vệ khi
/// thiếu thông tin người thực hiện (Service bị gọi trực tiếp không qua Controller).
/// </summary>
public class ReworkUnlockServiceTests
{
    private const int StageId = 100;

    private readonly Mock<IRepository<WorkStation>> _workStationRepositoryMock = new();
    private readonly Mock<IRepository<Scan>> _scanRepositoryMock = new();
    private readonly Mock<IRepository<ReworkUnlock>> _reworkUnlockRepositoryMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly ReworkUnlockService _sut;

    private List<ReworkUnlock> _existingUnlocks = new();

    public ReworkUnlockServiceTests()
    {
        _unitOfWorkMock.Setup(u => u.Repository<WorkStation>()).Returns(_workStationRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<Scan>()).Returns(_scanRepositoryMock.Object);
        _unitOfWorkMock.Setup(u => u.Repository<ReworkUnlock>()).Returns(_reworkUnlockRepositoryMock.Object);

        _sut = new ReworkUnlockService(_unitOfWorkMock.Object);

        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkStation { Id = 1, LineId = 1, StageId = StageId, Name = "Trạm 1" });

        SetupExistingUnlocks(new List<ReworkUnlock>());
        _reworkUnlockRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<ReworkUnlock>(), It.IsAny<CancellationToken>()))
            .Callback((ReworkUnlock u, CancellationToken _) => { u.Id = _existingUnlocks.Count + 1; _existingUnlocks.Add(u); })
            .Returns(Task.CompletedTask);
    }

    private void SetupExistingScans(List<Scan> existingScans)
    {
        _scanRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Scan, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<Scan, bool>> predicate, CancellationToken _) =>
                existingScans.Where(predicate.Compile()).ToList());
    }

    private void SetupExistingUnlocks(List<ReworkUnlock> existingUnlocks)
    {
        _existingUnlocks = existingUnlocks;
        _reworkUnlockRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ReworkUnlock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<ReworkUnlock, bool>> predicate, CancellationToken _) =>
                _existingUnlocks.Where(predicate.Compile()).ToList());
    }

    // AC2: tem đang bị khóa (Ng gần nhất chưa mở khóa) -> Tổ trưởng mở khóa thành công, ghi log đầy đủ (ai duyệt, thời điểm, ghi chú).
    [Fact]
    public async Task UnlockAsync_TemDangBiKhoa_MoKhoaThanhCongVaGhiLogDayDu()
    {
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow.AddMinutes(-5) } });

        var result = await _sut.UnlockAsync(1, "TAG1", "Đã sửa lỗi ngoại quan", unlockedByUserId: 9, unlockedByUserName: "totruong1");

        Assert.Equal("TAG1", result.TagCode);
        Assert.Equal(StageId, result.StageId);
        Assert.Equal(9, result.UnlockedByUserId);
        Assert.Equal("totruong1", result.UnlockedByUserName);
        Assert.Equal("Đã sửa lỗi ngoại quan", result.Note);
        _reworkUnlockRepositoryMock.Verify(r => r.AddAsync(
            It.Is<ReworkUnlock>(u => u.TagCode == "TAG1" && u.StageId == StageId && u.UnlockedByUserId == 9 && u.UnlockedByUserName == "totruong1" && u.Note == "Đã sửa lỗi ngoại quan"),
            It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // AC2: ghi chú tùy chọn -> để trống vẫn hợp lệ, lưu null (không lưu chuỗi rỗng).
    [Fact]
    public async Task UnlockAsync_KhongNhapGhiChu_LuuNoteNull()
    {
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow.AddMinutes(-5) } });

        var result = await _sut.UnlockAsync(1, "TAG1", note: null, unlockedByUserId: 9, unlockedByUserName: "totruong1");

        Assert.Null(result.Note);
    }

    // Tem chưa từng bị Ng -> không đang khóa -> từ chối mở khóa (không tạo bản ghi ReworkUnlock nào).
    [Fact]
    public async Task UnlockAsync_TemChuaTungBiNg_NemBusinessRuleException()
    {
        SetupExistingScans(new List<Scan>());

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UnlockAsync(1, "TAG-MOI", null, 9, "totruong1"));
        _reworkUnlockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ReworkUnlock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Tem đã Ng nhưng ĐÃ được mở khóa từ trước (không có Ng mới sau đó) -> không còn đang khóa -> từ chối mở khóa lần nữa.
    [Fact]
    public async Task UnlockAsync_TemDaDuocMoKhoaTuTruoc_NemBusinessRuleException()
    {
        var ngAt = DateTime.UtcNow.AddMinutes(-10);
        var unlockAt = DateTime.UtcNow.AddMinutes(-5);
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ngAt } });
        SetupExistingUnlocks(new List<ReworkUnlock> { new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlockAt, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" } });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UnlockAsync(1, "TAG1", null, 9, "totruong1"));
        _reworkUnlockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ReworkUnlock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // AC4: tem đã mở khóa rồi lại Ng lần 2 -> đang khóa lại -> mở khóa lần 2 hợp lệ, tạo thêm 1 bản ghi ReworkUnlock mới (không ghi đè bản ghi lần 1).
    [Fact]
    public async Task UnlockAsync_NgLanThuHaiSauKhiDaMoKhoaLanDau_MoKhoaLanHaiThanhCongVaGiuLaiLogLanDau()
    {
        var ng1At = DateTime.UtcNow.AddMinutes(-30);
        var unlock1At = DateTime.UtcNow.AddMinutes(-20);
        var ng2At = DateTime.UtcNow.AddMinutes(-5);
        SetupExistingScans(new List<Scan>
        {
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ng1At },
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ng2At },
        });
        SetupExistingUnlocks(new List<ReworkUnlock> { new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlock1At, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" } });

        var result = await _sut.UnlockAsync(1, "TAG1", "Sửa lần 2", unlockedByUserId: 11, unlockedByUserName: "totruong2");

        Assert.Equal(11, result.UnlockedByUserId);
        // AC5: 2 bản ghi ReworkUnlock (lần 1 + lần 2 vừa tạo) đều còn nguyên — không ghi đè.
        Assert.Equal(2, _existingUnlocks.Count);
        Assert.Contains(_existingUnlocks, u => u.UnlockedByUserId == 9);
        Assert.Contains(_existingUnlocks, u => u.UnlockedByUserId == 11 && u.Note == "Sửa lần 2");
    }

    // Phòng vệ: thiếu thông tin người thực hiện (Service bị gọi trực tiếp không qua Controller) -> từ chối, không tạo bản ghi.
    [Theory]
    [InlineData(0, "totruong1")]
    [InlineData(-1, "totruong1")]
    [InlineData(9, "")]
    [InlineData(9, " ")]
    public async Task UnlockAsync_ThieuThongTinNguoiThucHien_NemBusinessRuleException(int unlockedByUserId, string unlockedByUserName)
    {
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = DateTime.UtcNow.AddMinutes(-5) } });

        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.UnlockAsync(1, "TAG1", null, unlockedByUserId, unlockedByUserName));
        _reworkUnlockRepositoryMock.Verify(r => r.AddAsync(It.IsAny<ReworkUnlock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Trạm không tồn tại -> EntityNotFoundException.
    [Fact]
    public async Task UnlockAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.UnlockAsync(999, "TAG1", null, 9, "totruong1"));
    }

    // --- GetLockStatusAsync (feedback 18/08/2026: hiển thị thông tin lỗi NG trên màn "Mở khóa rework") ---

    // Tem chưa từng NG tại công đoạn này -> HasNgHistory=false, IsLocked=false, không có thông tin lỗi nào.
    [Fact]
    public async Task GetLockStatusAsync_TemChuaTungBiNg_TraVeHasNgHistoryFalseVaKhongDangKhoa()
    {
        SetupExistingScans(new List<Scan>());

        var result = await _sut.GetLockStatusAsync(1, "TAG-MOI");

        Assert.Equal("TAG-MOI", result.TagCode);
        Assert.Equal(StageId, result.StageId);
        Assert.False(result.HasNgHistory);
        Assert.False(result.IsLocked);
        Assert.Null(result.RejectionReason);
        Assert.Null(result.NgScannedAtUtc);
        Assert.Null(result.NgConfirmedByUserName);
        Assert.Equal(0, result.NgCount);
    }

    // Tem đang bị khóa (Ng gần nhất chưa mở khóa) -> IsLocked=true, thông tin lỗi lấy đúng từ bản ghi Ng gần nhất.
    [Fact]
    public async Task GetLockStatusAsync_TemDangBiKhoa_TraVeIsLockedTrueVaThongTinNgGanNhat()
    {
        var ngAt = DateTime.UtcNow.AddMinutes(-5);
        SetupExistingScans(new List<Scan>
        {
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ngAt, RejectionReason = "Trầy xước bề mặt", ConfirmedByUserId = 9, ConfirmedByUserName = "totruong1" },
        });

        var result = await _sut.GetLockStatusAsync(1, "TAG1");

        Assert.True(result.HasNgHistory);
        Assert.True(result.IsLocked);
        Assert.Equal("Trầy xước bề mặt", result.RejectionReason);
        Assert.Equal(ngAt, result.NgScannedAtUtc);
        Assert.Equal("totruong1", result.NgConfirmedByUserName);
        Assert.Equal(1, result.NgCount);
    }

    // Tem đã Ng nhưng ĐÃ được mở khóa từ trước (không có Ng mới sau đó) -> IsLocked=false, nhưng HasNgHistory vẫn true.
    [Fact]
    public async Task GetLockStatusAsync_TemDaDuocMoKhoaTuTruoc_TraVeIsLockedFalseNhungVanCoLichSuNg()
    {
        var ngAt = DateTime.UtcNow.AddMinutes(-10);
        var unlockAt = DateTime.UtcNow.AddMinutes(-5);
        SetupExistingScans(new List<Scan> { new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ngAt, RejectionReason = "Lỗi hàn", ConfirmedByUserId = 9, ConfirmedByUserName = "totruong1" } });
        SetupExistingUnlocks(new List<ReworkUnlock> { new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlockAt, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" } });

        var result = await _sut.GetLockStatusAsync(1, "TAG1");

        Assert.True(result.HasNgHistory);
        Assert.False(result.IsLocked);
        Assert.Equal("Lỗi hàn", result.RejectionReason);
        Assert.Equal(1, result.NgCount);
    }

    // NgCount đếm đúng tổng số lượt Ng (không chỉ lượt gần nhất), kể cả khi có xen kẽ 1 lượt Ok.
    [Fact]
    public async Task GetLockStatusAsync_NhieuLuotNg_NgCountDemDungTongSoLuotNg()
    {
        var ng1At = DateTime.UtcNow.AddMinutes(-30);
        var unlock1At = DateTime.UtcNow.AddMinutes(-20);
        var ng2At = DateTime.UtcNow.AddMinutes(-10);
        var okAt = DateTime.UtcNow.AddMinutes(-1);
        SetupExistingScans(new List<Scan>
        {
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ng1At, RejectionReason = "Lỗi lần 1", ConfirmedByUserId = 9, ConfirmedByUserName = "totruong1" },
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ng, ScannedAtUtc = ng2At, RejectionReason = "Lỗi lần 2", ConfirmedByUserId = 11, ConfirmedByUserName = "totruong2" },
            new() { TagCode = "TAG1", StageId = StageId, Result = ScanResult.Ok, ScannedAtUtc = okAt },
        });
        SetupExistingUnlocks(new List<ReworkUnlock> { new() { Id = 1, TagCode = "TAG1", StageId = StageId, UnlockedAtUtc = unlock1At, UnlockedByUserId = 9, UnlockedByUserName = "totruong1" } });

        var result = await _sut.GetLockStatusAsync(1, "TAG1");

        Assert.Equal(2, result.NgCount);
        // Bản ghi Ng gần nhất (ng2At) là bản đang khóa (chưa có ReworkUnlock nào sau đó).
        Assert.True(result.IsLocked);
        Assert.Equal("Lỗi lần 2", result.RejectionReason);
        Assert.Equal("totruong2", result.NgConfirmedByUserName);
    }

    // Trạm không tồn tại -> EntityNotFoundException, cùng hành vi UnlockAsync.
    [Fact]
    public async Task GetLockStatusAsync_TramKhongTonTai_NemEntityNotFoundException()
    {
        _workStationRepositoryMock.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync((WorkStation?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _sut.GetLockStatusAsync(999, "TAG1"));
    }

    // Mã tem để trống -> BusinessRuleException.
    [Fact]
    public async Task GetLockStatusAsync_MaTemRong_NemBusinessRuleException()
    {
        await Assert.ThrowsAsync<BusinessRuleException>(() => _sut.GetLockStatusAsync(1, "  "));
    }
}
