namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror rút gọn của RFC 7807 <c>ProblemDetails</c> mà API trả về cho lỗi 400/404/409/500 (API-Conventions.md mục 6).</summary>
public class ApiProblemDetails
{
    public int Status { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Detail { get; set; }

    public Dictionary<string, string[]>? Errors { get; set; }
}
