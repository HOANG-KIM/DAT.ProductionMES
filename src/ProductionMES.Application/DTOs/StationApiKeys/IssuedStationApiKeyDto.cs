namespace ProductionMES.Application.DTOs.StationApiKeys;

/// <summary>
/// Kết quả trả về ngay sau khi cấp/cấp lại API Key (US-04a AC1/AC4) — DUY NHẤT nơi <see cref="ApiKey"/> (giá
/// trị thô) xuất hiện; không có API nào khác trả lại được giá trị này (AC2). Admin phải sao chép ngay vào file
/// cấu hình cục bộ của trạm.
/// </summary>
public class IssuedStationApiKeyDto
{
    public int Id { get; set; }

    public int WorkStationId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Giá trị thô của API key — chỉ xuất hiện đúng 1 lần trong response này (AC1/AC2).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
