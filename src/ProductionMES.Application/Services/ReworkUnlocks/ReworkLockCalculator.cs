using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.ReworkUnlocks;

/// <summary>
/// Suy luận thuần túy (static, không phụ thuộc repository/DB) trạng thái "tem đang bị khóa rework hay không" tại
/// 1 (TagCode, StageId) — US-19 AC1/AC2. Tách riêng khỏi <c>ScanService</c>/<c>ReworkUnlockService</c> để test độc
/// lập dễ dàng (cùng idiom <c>AndonBoardCalculator</c>, US-09).
/// </summary>
public static class ReworkLockCalculator
{
    /// <summary>
    /// true nếu bản ghi <see cref="Scan"/> mới nhất (theo <see cref="Scan.ScannedAtUtc"/>) trong
    /// <paramref name="scansAtTagAndStage"/> có <see cref="ScanResult.Ng"/> VÀ không có <see cref="ReworkUnlock"/>
    /// nào trong <paramref name="unlocksAtTagAndStage"/> diễn ra SAU thời điểm Ng đó (xem remarks tại entity
    /// <see cref="ReworkUnlock"/>). Caller chịu trách nhiệm lọc sẵn 2 danh sách đúng theo (TagCode, StageId) —
    /// hàm này không tự lọc.
    /// </summary>
    public static bool IsLocked(IReadOnlyList<Scan> scansAtTagAndStage, IReadOnlyList<ReworkUnlock> unlocksAtTagAndStage)
    {
        var latestNg = scansAtTagAndStage
            .Where(s => s.Result == ScanResult.Ng)
            .OrderByDescending(s => s.ScannedAtUtc)
            .ThenByDescending(s => s.Id)
            .FirstOrDefault();

        if (latestNg is null)
        {
            // Chưa từng có lượt Ng nào tại (TagCode, StageId) này -> chưa từng bị khóa.
            return false;
        }

        var latestUnlock = unlocksAtTagAndStage
            .OrderByDescending(u => u.UnlockedAtUtc)
            .ThenByDescending(u => u.Id)
            .FirstOrDefault();

        // AC4: mỗi lần NG mới lại cần 1 lần mở khóa mới -> chỉ coi là "đã mở khóa" nếu lần mở khóa gần nhất diễn
        // ra SAU (>=) lần Ng gần nhất.
        return latestUnlock is null || latestUnlock.UnlockedAtUtc < latestNg.ScannedAtUtc;
    }
}
