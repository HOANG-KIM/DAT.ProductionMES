namespace ProductionMES.Station.Wpf.Models;

/// <summary>Mirror JSON của <c>CheckScanRequest</c> phía backend (US-12 AC3/AC4) — <c>POST api/v1/scans/check</c> (kiểm tra tem, KHÔNG lưu).</summary>
public class CheckScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Chỉ để server đối chiếu chống giả danh (ADR-005 AC6) — nguồn thật là claim của API key.</summary>
    public int WorkStationId { get; set; }
}
