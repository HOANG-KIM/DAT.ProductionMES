using ProductionMES.Station.Wpf.Models;

namespace ProductionMES.Station.Wpf.Services.PackingBoxes;

/// <summary>
/// Gọi <c>api/v1/packing-boxes*</c> (US-25) — <see cref="GetStateAsync"/>/<see cref="SetStartingBoxNoAsync"/>/
/// <see cref="DownloadLabelAsync"/> dùng scheme "StationApiKey" (ADR-005, không cần đăng nhập Supervisor, cùng
/// <c>IScanApiClient</c>); <see cref="UpdateCurrentBoxNoAsync"/> cần Bearer Tổ trưởng dùng RIÊNG cho 1 thao tác
/// (re-auth mỗi lần, cùng cách <c>IScanApiClient.CreateNgAsync</c> nhận <c>supervisorAccessToken</c> tường minh,
/// KHÔNG dùng <c>SupervisorAuthHandler</c>/phiên dùng chung).
/// </summary>
public interface IPackingBoxApiClient
{
    /// <summary>AC5/AC6/AC9: trạng thái đóng thùng hiện tại của trạm.</summary>
    Task<PackingBoxStateDto> GetStateAsync(CancellationToken cancellationToken = default);

    /// <summary>AC5: nhập số thùng bắt đầu.</summary>
    Task<PackingBoxDto> SetStartingBoxNoAsync(int startingBoxNo, CancellationToken cancellationToken = default);

    /// <summary>AC7: sửa số thùng hiện tại — cần <paramref name="supervisorAccessToken"/> (đã re-auth, permission <c>PackingBox.Update</c>).</summary>
    Task<PackingBoxDto> UpdateCurrentBoxNoAsync(int workStationId, int newBoxNo, string supervisorAccessToken, CancellationToken cancellationToken = default);

    // US-27 (25/08/2026): ConfirmDuplicateAsync (US-25 AC8) đã bị XÓA — SUPERSEDED bởi
    // IScanApiClient.ConfirmRejectedScanAsync (áp dụng đồng nhất cho mọi ScanResult từ chối tự động, xem US-27 AC12).

    /// <summary>AC4/AC13: tải file tem thùng đã merge dữ liệu (xlsx), ghi ra <paramref name="destinationFilePath"/> — dùng cho in tự động lẫn "In lại" thủ công.</summary>
    Task DownloadLabelAsync(int boxId, string destinationFilePath, CancellationToken cancellationToken = default);
}
