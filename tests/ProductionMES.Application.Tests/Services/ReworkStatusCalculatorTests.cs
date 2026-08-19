using ProductionMES.Application.Services.ReworkUnlocks;
using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Tests.Services;

/// <summary>
/// Unit test thuần túy cho <see cref="ReworkStatusCalculator.Compute"/> (US-21 AC10) — không phụ thuộc
/// repository/DB, cùng idiom <c>ReworkLockCalculatorTests</c> (US-19).
/// </summary>
public class ReworkStatusCalculatorTests
{
    private static Scan MakeScan(int id, DateTime scannedAtUtc, ScanResult result)
        => new() { Id = id, TagCode = "TAG1", StageId = 1, ScannedAtUtc = scannedAtUtc, Result = result };

    private static ReworkUnlock MakeUnlock(int id, DateTime unlockedAtUtc, string unlockedByUserName = "supervisor1", string? note = null)
        => new() { Id = id, TagCode = "TAG1", StageId = 1, UnlockedAtUtc = unlockedAtUtc, UnlockedByUserId = 1, UnlockedByUserName = unlockedByUserName, Note = note };

    // AC10 trạng thái 1: "Chưa mở khóa" — không có ReworkUnlock nào sau lượt NG này.
    [Fact]
    public void Compute_KhongCoReworkUnlockNaoSauLuotNg_ChuaMoKhoa()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var ngScan = MakeScan(1, ngAt, ScanResult.Ng);
        var scans = new List<Scan> { ngScan };
        var unlocks = new List<ReworkUnlock>();

        var result = ReworkStatusCalculator.Compute(ngScan, scans, unlocks);

        Assert.Equal(ReworkStatus.NotUnlocked, result.Status);
        Assert.Null(result.Unlock);
        Assert.Null(result.StillNgOccurrence);
    }

    // AC10 trạng thái 2: "Đã mở khóa, chờ scan lại" — có ReworkUnlock sau lượt NG, chưa có scan nào mới hơn.
    [Fact]
    public void Compute_DaMoKhoaChuaCoScanMoiHon_ChoScanLai()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddMinutes(5);
        var ngScan = MakeScan(1, ngAt, ScanResult.Ng);
        var scans = new List<Scan> { ngScan };
        var unlock = MakeUnlock(1, unlockAt, "to_truong_a", "Đã thay linh kiện lỗi.");
        var unlocks = new List<ReworkUnlock> { unlock };

        var result = ReworkStatusCalculator.Compute(ngScan, scans, unlocks);

        Assert.Equal(ReworkStatus.WaitingRescan, result.Status);
        Assert.Same(unlock, result.Unlock);
        Assert.Null(result.StillNgOccurrence);
    }

    // AC10 trạng thái 3: "Đã sửa xong (scan lại OK)" — có scan mới hơn ReworkUnlock gần nhất với Result = Ok.
    [Fact]
    public void Compute_CoScanOkSauKhiMoKhoa_DaSuaXong()
    {
        var ngAt = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ngAt.AddMinutes(5);
        var okAt = unlockAt.AddMinutes(5);
        var ngScan = MakeScan(1, ngAt, ScanResult.Ng);
        var okScan = MakeScan(2, okAt, ScanResult.Ok);
        var scans = new List<Scan> { ngScan, okScan };
        var unlock = MakeUnlock(1, unlockAt);
        var unlocks = new List<ReworkUnlock> { unlock };

        var result = ReworkStatusCalculator.Compute(ngScan, scans, unlocks);

        Assert.Equal(ReworkStatus.Fixed, result.Status);
        Assert.Same(unlock, result.Unlock);
        Assert.Null(result.StillNgOccurrence);
    }

    // AC10 trạng thái 4: "Đã scan lại nhưng vẫn NG (lần N)" — có scan mới hơn ReworkUnlock gần nhất nhưng vẫn Ng.
    [Fact]
    public void Compute_CoScanNgSauKhiMoKhoa_VanNgKemSoLan()
    {
        var ng1At = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlockAt = ng1At.AddMinutes(5);
        var ng2At = unlockAt.AddMinutes(5);
        var ng1Scan = MakeScan(1, ng1At, ScanResult.Ng);
        var ng2Scan = MakeScan(2, ng2At, ScanResult.Ng);
        var scans = new List<Scan> { ng1Scan, ng2Scan };
        var unlock = MakeUnlock(1, unlockAt);
        var unlocks = new List<ReworkUnlock> { unlock };

        var result = ReworkStatusCalculator.Compute(ng1Scan, scans, unlocks);

        Assert.Equal(ReworkStatus.StillNg, result.Status);
        Assert.Same(unlock, result.Unlock);
        // ng2Scan là lượt NG thứ 2 trong toàn bộ lịch sử (TagCode, StageId) này -> "lần 2".
        Assert.Equal(2, result.StillNgOccurrence);
    }

    // Đánh giá đúng lượt NG CŨ trong lịch sử nhiều lần NG/mở khóa xen kẽ — không gán nhầm unlock của lần NG SAU cho lần NG cũ hơn.
    [Fact]
    public void Compute_LuotNgCuTrongLichSuNhieuLanNgMoKhoaXenKe_GanDungReworkUnlockTuongUng()
    {
        var ng1At = new DateTime(2026, 8, 18, 8, 0, 0, DateTimeKind.Utc);
        var unlock1At = ng1At.AddMinutes(5);
        var okAt = unlock1At.AddMinutes(5);
        var ng2At = okAt.AddMinutes(5); // Ng lần 2 xảy ra ở công đoạn kế tiếp không liên quan tới lượt Ng lần 1 nữa.
        var unlock2At = ng2At.AddMinutes(5);

        var ng1Scan = MakeScan(1, ng1At, ScanResult.Ng);
        var okScan = MakeScan(2, okAt, ScanResult.Ok);
        var ng2Scan = MakeScan(3, ng2At, ScanResult.Ng);
        var scans = new List<Scan> { ng1Scan, okScan, ng2Scan };

        var unlock1 = MakeUnlock(1, unlock1At, "to_truong_a");
        var unlock2 = MakeUnlock(2, unlock2At, "to_truong_b");
        var unlocks = new List<ReworkUnlock> { unlock1, unlock2 };

        // Lượt Ng lần 1 (cũ) phải gắn đúng unlock1 (gần nó nhất), phản ánh đã sửa xong (scan lại Ok).
        var result1 = ReworkStatusCalculator.Compute(ng1Scan, scans, unlocks);
        Assert.Equal(ReworkStatus.Fixed, result1.Status);
        Assert.Same(unlock1, result1.Unlock);

        // Lượt Ng lần 2 (mới hơn) phải gắn đúng unlock2, đang chờ scan lại.
        var result2 = ReworkStatusCalculator.Compute(ng2Scan, scans, unlocks);
        Assert.Equal(ReworkStatus.WaitingRescan, result2.Status);
        Assert.Same(unlock2, result2.Unlock);
    }
}
