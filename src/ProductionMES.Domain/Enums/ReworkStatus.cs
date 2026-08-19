namespace ProductionMES.Domain.Enums;

/// <summary>
/// US-21 AC10 — trạng thái rework của 1 lượt scan cụ thể có <see cref="ScanResult.Ng"/>, SUY LUẬN ĐỘNG mỗi lần
/// truy vấn báo cáo (xem <c>ProductionMES.Application.Services.ReworkUnlocks.ReworkStatusCalculator</c>) — KHÔNG
/// phải cột lưu trữ ở entity nào (khác các enum khác trong thư mục này, đều gắn với 1 trạng thái PERSISTED). Đặt
/// cùng chỗ với các enum khác để nhất quán vị trí, dù bản chất là 1 giá trị tính toán (derived).
/// </summary>
public enum ReworkStatus
{
    /// <summary>Chưa có <c>ReworkUnlock</c> nào diễn ra sau (≥) thời điểm lượt NG này.</summary>
    NotUnlocked = 0,

    /// <summary>Đã có ≥1 <c>ReworkUnlock</c> sau lượt NG này, nhưng chưa có lượt scan nào mới hơn tại cùng (TagCode, StageId).</summary>
    WaitingRescan = 1,

    /// <summary>Có lượt scan mới hơn <c>ReworkUnlock</c> gần nhất với <see cref="ScanResult.Ok"/>.</summary>
    Fixed = 2,

    /// <summary>Có lượt scan mới hơn <c>ReworkUnlock</c> gần nhất nhưng vẫn <see cref="ScanResult.Ng"/>.</summary>
    StillNg = 3,
}
