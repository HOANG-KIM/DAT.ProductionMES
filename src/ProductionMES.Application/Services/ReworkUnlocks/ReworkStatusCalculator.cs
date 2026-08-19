using ProductionMES.Domain.Entities;
using ProductionMES.Domain.Enums;

namespace ProductionMES.Application.Services.ReworkUnlocks;

/// <summary>
/// Suy luận thuần túy (static, không phụ thuộc repository/DB) trạng thái rework (US-21 AC10) cho 1 lượt
/// <see cref="Scan"/> NG cụ thể. Tách riêng khỏi <see cref="ReworkLockCalculator"/> (US-19) vì mục đích khác:
/// <see cref="ReworkLockCalculator.IsLocked"/> chỉ trả lời true/false "tem hiện có đang bị khóa hay không" (dựa
/// trên lượt Ng MỚI NHẤT), còn hàm này trả lời "trạng thái rework của ĐÚNG lượt NG này" (có thể là 1 lượt NG cũ
/// hơn trong lịch sử, đã hoặc chưa được xử lý) — cần thiết cho AC10/AC11 hiển thị đầy đủ lịch sử rework theo Lot,
/// không chỉ trạng thái mới nhất. Cùng idiom (static, pure, test độc lập dễ dàng) và tái sử dụng đúng quy ước so
/// sánh mốc thời gian &gt;= đã chốt ở <see cref="ReworkLockCalculator"/> (1 <c>ReworkUnlock</c> tính là "xử lý"
/// cho 1 lượt NG nếu <c>UnlockedAtUtc</c> &gt;= <c>ScannedAtUtc</c> của lượt NG đó).
/// </summary>
public static class ReworkStatusCalculator
{
    /// <summary>Kết quả suy luận cho 1 lượt NG cụ thể — <see cref="Unlock"/>/<see cref="StillNgOccurrence"/> chỉ có giá trị tùy theo <see cref="Status"/> (xem property).</summary>
    public sealed record ReworkStatusResult(ReworkStatus Status, ReworkUnlock? Unlock, int? StillNgOccurrence);

    /// <summary>
    /// Caller chịu trách nhiệm lọc sẵn <paramref name="scansAtTagAndStage"/>/<paramref name="unlocksAtTagAndStage"/>
    /// đúng theo (<c>ngScan.TagCode</c>, <c>ngScan.StageId</c>) — hàm này không tự lọc. <paramref name="ngScan"/>
    /// PHẢI có <c>Result == Ng</c> (caller đảm bảo, không tự kiểm tra lại ở đây).
    /// </summary>
    public static ReworkStatusResult Compute(Scan ngScan, IReadOnlyList<Scan> scansAtTagAndStage, IReadOnlyList<ReworkUnlock> unlocksAtTagAndStage)
    {
        // AC10 "Chưa mở khóa": tìm ReworkUnlock GẦN LƯỢT NG NÀY NHẤT (sớm nhất sau đó) — không phải unlock mới
        // nhất tuyệt đối trong toàn bộ lịch sử (tránh gán nhầm 1 unlock của lần NG SAU cho lần NG cũ hơn này khi
        // 1 tem có nhiều lần NG liên tiếp). Ngưỡng >= giống hệt ReworkLockCalculator.IsLocked.
        var unlockAfter = unlocksAtTagAndStage
            .Where(u => u.UnlockedAtUtc >= ngScan.ScannedAtUtc)
            .OrderBy(u => u.UnlockedAtUtc).ThenBy(u => u.Id)
            .FirstOrDefault();

        if (unlockAfter is null)
        {
            return new ReworkStatusResult(ReworkStatus.NotUnlocked, null, null);
        }

        // AC10 "Đã sửa xong"/"vẫn NG": lượt scan (chỉ tính Ok/Ng — 2 kết quả nghiệp vụ thật, bỏ qua
        // DuplicateTag/PreviousStageNotPassed/WaitingReworkUnlock vì không phải kết quả xử lý rework thật) sớm
        // nhất sau ReworkUnlock vừa tìm được, loại trừ chính ngScan (phòng trường hợp trùng mốc thời gian hiếm gặp).
        var nextScan = scansAtTagAndStage
            .Where(s => s.Id != ngScan.Id && (s.Result == ScanResult.Ok || s.Result == ScanResult.Ng) && s.ScannedAtUtc >= unlockAfter.UnlockedAtUtc)
            .OrderBy(s => s.ScannedAtUtc).ThenBy(s => s.Id)
            .FirstOrDefault();

        if (nextScan is null)
        {
            return new ReworkStatusResult(ReworkStatus.WaitingRescan, unlockAfter, null);
        }

        if (nextScan.Result == ScanResult.Ok)
        {
            return new ReworkStatusResult(ReworkStatus.Fixed, unlockAfter, null);
        }

        // "lần N": thứ tự lượt NG này trong TOÀN BỘ lịch sử NG tại (TagCode, StageId), tính từ lượt NG đầu tiên (N=1).
        var ngOccurrence = scansAtTagAndStage
            .Where(s => s.Result == ScanResult.Ng)
            .OrderBy(s => s.ScannedAtUtc).ThenBy(s => s.Id)
            .ToList()
            .FindIndex(s => s.Id == nextScan.Id) + 1;

        return new ReworkStatusResult(ReworkStatus.StillNg, unlockAfter, ngOccurrence);
    }
}
