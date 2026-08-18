namespace ProductionMES.Application.DTOs.ReworkUnlocks;

/// <summary>
/// Thông tin lỗi NG gần nhất + trạng thái khóa rework hiện tại của 1 tem tại đúng công đoạn của 1 trạm — phục vụ
/// màn "Mở khóa rework" (US-19, feedback 18/08/2026) hiển thị cho Tổ trưởng TRƯỚC khi quyết định mở khóa, không
/// phải nhớ hoặc tra ở nơi khác. Chỉ mang tính tham khảo, KHÔNG dùng để chặn <c>UnlockAsync</c> — endpoint mở khóa
/// vẫn tự kiểm tra lại <see cref="Services.ReworkUnlocks.ReworkLockCalculator.IsLocked"/> độc lập.
/// </summary>
public class ReworkLockStatusDto
{
    public string TagCode { get; set; } = string.Empty;

    public int StageId { get; set; }

    /// <summary>true nếu tem hiện đang bị khóa rework tại công đoạn này (chưa từng NG thì luôn false).</summary>
    public bool IsLocked { get; set; }

    /// <summary>true nếu tem này đã từng có ít nhất 1 lượt NG tại công đoạn này (dù đang khóa hay đã mở khóa).</summary>
    public bool HasNgHistory { get; set; }

    /// <summary><see cref="Domain.Entities.Scan.RejectionReason"/> của lượt NG gần nhất — null nếu <see cref="HasNgHistory"/> = false.</summary>
    public string? RejectionReason { get; set; }

    /// <summary><see cref="Domain.Entities.Scan.ScannedAtUtc"/> của lượt NG gần nhất — null nếu <see cref="HasNgHistory"/> = false.</summary>
    public DateTime? NgScannedAtUtc { get; set; }

    /// <summary><see cref="Domain.Entities.Scan.ConfirmedByUserName"/> của lượt NG gần nhất — null nếu <see cref="HasNgHistory"/> = false.</summary>
    public string? NgConfirmedByUserName { get; set; }

    /// <summary>Tổng số lượt NG (Result = Ng) đã ghi nhận cho tem này tại công đoạn này, tính đến thời điểm tra cứu.</summary>
    public int NgCount { get; set; }
}
