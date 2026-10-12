namespace ProductionMES.Application.DTOs.Scans;

/// <summary>
/// Request lưu Scan OK gắn với kết quả của máy thông điện (US-12 AC2, <c>POST api/v1/scans/tester</c>). Chỉ gọi SAU khi
/// máy đã trả <c>#BIND_OK</c> cho đúng tem này. Bộ khóa (<see cref="TesterBootId"/>, <see cref="TesterSeq"/>) cùng trạm
/// dùng để chống lưu trùng khi retry (AC15).
/// </summary>
public class CreateTesterScanRequest
{
    public string TagCode { get; set; } = string.Empty;

    /// <summary>Field cấp cao để đối chiếu chống giả danh ở tầng xác thực (ADR-005 AC6) — xem <see cref="CheckScanRequest.WorkStationId"/>.</summary>
    public int WorkStationId { get; set; }

    /// <summary><c>BOOT</c> của máy (HELLO) — 8 ký tự hex.</summary>
    public string TesterBootId { get; set; } = string.Empty;

    /// <summary><c>SEQ</c> của <c>#RESULT</c> đã được BIND_OK.</summary>
    public int TesterSeq { get; set; }

    /// <summary><c>GM</c> của <c>#RESULT</c> (4 ký tự hex).</summary>
    public string GoldenCrc { get; set; } = string.Empty;

    /// <summary><c>FW</c> của HELLO gần nhất.</summary>
    public string FirmwareVersion { get; set; } = string.Empty;

    /// <summary>Thời điểm App nhận <c>#RESULT</c> (giờ local PC trạm).</summary>
    public DateTime TestedAt { get; set; }
}
