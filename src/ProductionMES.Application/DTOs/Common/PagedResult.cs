namespace ProductionMES.Application.DTOs.Common;

/// <summary>
/// Envelope phân trang chuẩn theo `Documents/API-Conventions.md` mục 9 — dùng chung cho mọi endpoint có phân
/// trang (US-10 là endpoint đầu tiên áp dụng, cho danh sách lịch sử scan có thể rất lớn).
/// </summary>
/// <typeparam name="TItem">Kiểu 1 phần tử trong trang kết quả.</typeparam>
public class PagedResult<TItem>
{
    public IReadOnlyList<TItem> Items { get; set; } = Array.Empty<TItem>();

    public int TotalCount { get; set; }

    /// <summary>Trang hiện tại, bắt đầu từ 1.</summary>
    public int Page { get; set; }

    public int PageSize { get; set; }
}
