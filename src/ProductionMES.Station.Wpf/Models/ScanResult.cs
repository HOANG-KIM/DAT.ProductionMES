namespace ProductionMES.Station.Wpf.Models;

/// <summary>
/// Mirror của <c>ProductionMES.Domain.Enums.ScanResult</c> phía backend (US-07/US-08, FR-08/FR-10) — Station.Wpf
/// không reference project backend nào (CLAUDE.md), nên định nghĩa lại đúng tên/giá trị để deserialize JSON dạng
/// chuỗi (API-Conventions.md mục 10).
/// </summary>
public enum ScanResult
{
    /// <summary>Lượt scan hợp lệ — qua đủ 2 bước kiểm tra (chống trùng tem + đã qua công đoạn liền trước).</summary>
    Ok = 0,

    /// <summary>Bị từ chối: tem đã được scan OK tại cùng công đoạn này ở bất kỳ Line nào (toàn hệ thống).</summary>
    DuplicateTag = 1,

    /// <summary>Bị từ chối: tem chưa từng scan OK tại công đoạn liền trước ở bất kỳ Line nào (toàn hệ thống).</summary>
    PreviousStageNotPassed = 2,
}
