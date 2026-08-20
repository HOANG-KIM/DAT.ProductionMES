namespace ProductionMES.Domain.Entities;

/// <summary>
/// Lịch sử thay đổi <see cref="Lot.TotalQuantity"/> — 1 dòng cho mỗi lần giá trị THỰC SỰ đổi (bỏ qua lần lưu lại
/// cùng giá trị cũ), phục vụ truy vết khi Tổ trưởng/Admin sửa "Tổng số lượng Lot" sau khi đã có lượt scan OK dựa
/// theo giá trị cũ (vd đặt 500 rồi sửa còn 100 — xem <see cref="Services.Lots.LotService.UpsertTotalQuantityAsync"/>
/// trong <c>ProductionMES.Application</c>).
/// </summary>
public class LotHistory
{
    public int Id { get; set; }

    /// <summary>Khớp <see cref="Lot.Code"/> — KHÔNG có FK (đúng convention dự án, xem CLAUDE.md mục Data access).</summary>
    public string LotCode { get; set; } = string.Empty;

    /// <summary><c>null</c> khi đây là lần đầu tiên nhập giá trị cho Lot này (chưa từng có row <see cref="Lot"/> trước đó).</summary>
    public int? OldTotalQuantity { get; set; }

    public int NewTotalQuantity { get; set; }

    /// <summary>
    /// Giờ tường tại nhà máy (giờ Việt Nam, UTC+7), KHÔNG quy đổi — cùng ngoại lệ đã chốt 19/08/2026 với
    /// <see cref="Lot.UpdatedAtUtc"/> (xem API-Conventions.md mục 10).
    /// </summary>
    public DateTime ChangedAtUtc { get; set; }

    /// <summary>Tên đăng nhập người thực hiện thay đổi (snapshot, không tra cứu động) — có thể null nếu không xác định được.</summary>
    public string? ChangedByUserName { get; set; }
}
