namespace ProductionMES.Domain.Enums;

/// <summary>
/// 4 nhóm vai trò người dùng theo đúng mục 2.2 SRS (US-22/FR-22). Không có vai trò "QC" riêng — mọi chức năng
/// trước đây gắn với "QC" thuộc về vai trò <see cref="Supervisor"/> (đã chốt tại mục 8.1 SRS, AC3 US-22).
/// </summary>
public enum UserRole
{
    /// <summary>Công nhân vận hành trạm — scan tem, xem số lượng/chỉ số +/- tại trạm phụ trách.</summary>
    Operator = 0,

    /// <summary>Tổ trưởng / Quản lý chuyền — cấu hình kế hoạch, công đoạn, trình tự; mở khóa rework; xem báo cáo.</summary>
    Supervisor = 1,

    /// <summary>Quản trị hệ thống — quản lý danh mục Line, công đoạn, trạm, người dùng.</summary>
    Admin = 2,

    /// <summary>Ban quản lý / Văn phòng — xem báo cáo tổng hợp toàn bộ Line theo thời gian thực.</summary>
    Manager = 3,
}
