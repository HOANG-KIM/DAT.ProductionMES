using ProductionMES.Application.Services.ReworkUnlocks;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test thuần túy cho <see cref="ReworkLockCalculator.IsLocked"/> (US-19 AC1/AC2/AC4/AC5) — không phụ thuộc
/// repository/DB, cùng idiom <c>AndonBoardCalculatorTests</c> (US-09).
/// </summary>
public class ReworkLockCalculatorTests
{
    private static Scan MakeScan(int id, DateTime scannedAtUtc, ScanResult result)
        => new() { Id = id, TagCode = "TAG1", StageId = 1, ScannedAtUtc = scannedAtUtc, Result = result };

    private static ReworkUnlock MakeUnlock(int id, DateTime unlockedAtUtc)
        => new() { Id = id, TagCode = "TAG1", StageId = 1, UnlockedAtUtc = unlockedAtUtc, UnlockedByUserId = 1, UnlockedByUserName = "supervisor1" };

    [Fact]
    public void IsLocked_ChuaTungCoLuotNgNao_KhongKhoa()
    {
        var scans = new List<Scan> { MakeScan(1, DateTime.UtcNow, ScanResult.Ok) };
        var unlocks = new List<ReworkUnlock>();

        Assert.False(ReworkLockCalculator.IsLocked(scans, unlocks));
    }

    // AC1: tem vừa bị Ng, chưa có ReworkUnlock nào -> đang khóa.
    [Fact]
    public void IsLocked_CoLuotNgGanNhatChuaMoKhoa_DangKhoa()
    {
        var now = DateTime.UtcNow;
        var scans = new List<Scan> { MakeScan(1, now, ScanResult.Ng) };
        var unlocks = new List<ReworkUnlock>();

        Assert.True(ReworkLockCalculator.IsLocked(scans, unlocks));
    }

    // AC2: đã có 1 lần mở khóa SAU lượt Ng gần nhất -> không còn khóa.
    [Fact]
    public void IsLocked_DaMoKhoaSauLuotNgGanNhat_KhongConKhoa()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddMinutes(5);
        var scans = new List<Scan> { MakeScan(1, ngAt, ScanResult.Ng) };
        var unlocks = new List<ReworkUnlock> { MakeUnlock(1, unlockAt) };

        Assert.False(ReworkLockCalculator.IsLocked(scans, unlocks));
    }

    // AC4: sau khi mở khóa, scan lại vẫn Ng lần 2 -> khóa lại (dù đã có 1 lần mở khóa TRƯỚC lượt Ng lần 2 này).
    [Fact]
    public void IsLocked_NgLanThuHaiSauKhiDaMoKhoaLanDau_KhoaLai()
    {
        var ng1At = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlock1At = ng1At.AddMinutes(5);
        var ng2At = unlock1At.AddMinutes(10);
        var scans = new List<Scan>
        {
            MakeScan(1, ng1At, ScanResult.Ng),
            MakeScan(2, ng2At, ScanResult.Ng),
        };
        var unlocks = new List<ReworkUnlock> { MakeUnlock(1, unlock1At) };

        Assert.True(ReworkLockCalculator.IsLocked(scans, unlocks));
    }

    // AC5: đã có Ok SAU khi mở khóa (tem đã qua công đoạn) -> Ng cũ không còn ý nghĩa khóa (không phải trạng thái
    // hiện tại), nhưng lượt Ng cũ vẫn còn trong danh sách (giữ nguyên lịch sử) — vẫn không khóa vì Ng không phải
    // bản ghi mới nhất.
    [Fact]
    public void IsLocked_DaCoOkSauKhiMoKhoa_KhongConKhoaVaGiuNguyenLichSuNgCu()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddMinutes(5);
        var okAt = unlockAt.AddMinutes(5);
        var scans = new List<Scan>
        {
            MakeScan(1, ngAt, ScanResult.Ng),
            MakeScan(2, okAt, ScanResult.Ok),
        };
        var unlocks = new List<ReworkUnlock> { MakeUnlock(1, unlockAt) };

        Assert.False(ReworkLockCalculator.IsLocked(scans, unlocks));
        // Lịch sử vẫn còn đủ 2 bản ghi (Ng cũ + Ok mới) — không bị caller xóa/ghi đè (kiểm tra ở đây chỉ là guard,
        // AC5 thật sự được đảm bảo bởi ScanService/ReworkUnlockService không bao giờ Remove/Update bản ghi Scan cũ).
        Assert.Equal(2, scans.Count);
    }

    // Nhiều lần mở khóa cho các lượt Ng khác nhau tại cùng (TagCode, StageId) -> chỉ so lần mở khóa GẦN NHẤT với lần Ng GẦN NHẤT.
    [Fact]
    public void IsLocked_NhieuLanUnlockNhungLanMoKhoaGanNhatTruocLanNgGanNhat_DangKhoa()
    {
        var ng1At = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlock1At = ng1At.AddMinutes(5);
        var ng2At = unlock1At.AddMinutes(10); // Ng lần 2, chưa có unlock nào sau đó.
        var scans = new List<Scan>
        {
            MakeScan(1, ng1At, ScanResult.Ng),
            MakeScan(2, ng2At, ScanResult.Ng),
        };
        var unlocks = new List<ReworkUnlock> { MakeUnlock(1, unlock1At) };

        Assert.True(ReworkLockCalculator.IsLocked(scans, unlocks));
    }
}
