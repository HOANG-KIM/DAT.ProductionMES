namespace ProductionMES.Domain.Enums;

/// <summary>
/// Kết quả 1 lượt scan (FR-08/FR-10, US-07/US-08). Thiết kế mở rộng được — US-18 sẽ bổ sung thêm giá trị
/// <c>Ng</c> (không đạt kiểm tra Arduino) sau này, không cần đổi lại các giá trị đã có ở đây.
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
