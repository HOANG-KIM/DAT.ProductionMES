namespace ProductionMES.Domain.Entities;

/// <summary>
/// Cấu hình Quy cách đóng gói theo Model (US-24/FR-24, công đoạn Đóng thùng) — mỗi Model sản phẩm có đúng 1 bộ
/// cấu hình: số lượng sản phẩm/thùng, khối lượng, tên sản phẩm, nhà sản xuất, và 1 file mẫu tem in (template)
/// dùng khi tự động in tem dán thùng (US-25/FR-25/FR-26).
/// </summary>
/// <remarks>
/// <see cref="Model"/> khớp giá trị <see cref="ProductionPlan.Model"/> (free-text hiện có, KHÔNG tách danh mục
/// riêng) — KHÔNG có FK/unique constraint kiểu truyền thống ở tầng DB (CLAUDE.md: hệ thống không dùng khoá
/// ngoại). Tính duy nhất theo Model (không phân biệt hoa/thường, tự trim khoảng trắng — AC9) do tầng Service
/// đảm bảo khi tạo mới, dựa trên <see cref="ModelNormalized"/> (snapshot đã chuẩn hoá — trim + upper invariant —
/// lưu SẴN thay vì tính lại mỗi lần tra cứu, để không phụ thuộc collation thật của MySQL, đúng quyết định đã
/// chốt với người giao việc). <see cref="Model"/> vẫn lưu nguyên văn (đúng hoa/thường/khoảng trắng người dùng đã
/// nhập) để hiển thị lại đúng như đã nhập.
///
/// File mẫu tem (template .xlsx) KHÔNG lưu BLOB trong MySQL — lưu trên filesystem server (xem
/// <c>IPackingTemplateStorage</c>, đặt tên file theo <see cref="Id"/> để tránh ký tự không hợp lệ trong
/// <see cref="Model"/> free-text). Entity chỉ lưu metadata: <see cref="HasTemplate"/>,
/// <see cref="TemplateUpdatedAtUtc"/>, <see cref="TemplateUpdatedByUserName"/>.
///
/// Sửa cấu hình KHÔNG hồi tố (AC2) — các thùng đã đóng/in tem trước đó không đọc lại cấu hình entity này (US-25
/// sẽ snapshot đúng giá trị tại thời điểm mở thùng, xem SRS mục 6 quy tắc 17). US-24 chỉ cần đảm bảo đọc ra giá
/// trị MỚI sau khi sửa, không cần xử lý gì thêm ở entity này.
/// </remarks>
public class PackingModelConfig
{
    public int Id { get; set; }

    /// <summary>Model sản phẩm — lưu nguyên văn theo đúng người dùng đã nhập (khớp <see cref="ProductionPlan.Model"/>).</summary>
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot đã chuẩn hoá (trim + <c>ToUpperInvariant()</c>) của <see cref="Model"/> — dùng để so khớp/tra cứu
    /// không phân biệt hoa/thường, tự trim khoảng trắng (AC9), KHÔNG phụ thuộc collation của MySQL.
    /// </summary>
    public string ModelNormalized { get; set; } = string.Empty;

    /// <summary>Quy cách đóng gói — số lượng sản phẩm tối đa/thùng (AC1/AC7, bắt buộc &gt; 0).</summary>
    public int PackingQuantity { get; set; }

    /// <summary>Khối lượng (gross weight) — không bắt buộc; nếu có nhập phải &gt; 0 (AC7).</summary>
    public decimal? GrossWeight { get; set; }

    /// <summary>Tên sản phẩm — bắt buộc (AC1/AC7).</summary>
    public string PartName { get; set; } = string.Empty;

    /// <summary>Nhà sản xuất — không bắt buộc.</summary>
    public string? Manufacturer { get; set; }

    /// <summary>true nếu đã có file mẫu tem (template .xlsx) được tải lên (AC3/AC4/AC5).</summary>
    public bool HasTemplate { get; set; }

    /// <summary>
    /// Thời điểm tải lên mẫu tem gần nhất — UTC thật (<c>DateTime.UtcNow</c>), theo đúng quy ước MẶC ĐỊNH của
    /// API-Conventions.md mục 10 (KHÔNG thuộc danh sách 4 field ngoại lệ giờ local đã chốt — Scan.ScannedAtUtc/
    /// ReworkUnlock.UnlockedAtUtc/Lot.UpdatedAtUtc/LotHistory.ChangedAtUtc).
    /// </summary>
    public DateTime? TemplateUpdatedAtUtc { get; set; }

    /// <summary>Tên đăng nhập người tải lên mẫu tem gần nhất (snapshot, không tra cứu động).</summary>
    public string? TemplateUpdatedByUserName { get; set; }

    /// <summary>Thời điểm tạo cấu hình — UTC thật (<c>DateTime.UtcNow</c>).</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Thời điểm cập nhật gần nhất (bất kỳ trường nào, không tính riêng template) — UTC thật (<c>DateTime.UtcNow</c>).</summary>
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>Tên đăng nhập người cập nhật gần nhất (snapshot).</summary>
    public string? UpdatedByUserName { get; set; }
}
