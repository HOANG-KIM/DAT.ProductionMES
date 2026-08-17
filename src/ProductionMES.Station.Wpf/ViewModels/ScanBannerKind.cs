namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// Trạng thái hiện tại của banner kết quả scan trên <c>AndonBoardWindow</c> (US-07 AC3/AC4/AC5) — quyết định
/// tiêu đề/màu/hành vi đóng, theo mockup đã chốt (memory <c>station-wpf-dashboard-mockup</c>): <c>OK INPUT</c>
/// (xanh, tự đóng) / <c>NG INPUT</c> (đỏ, chờ xác nhận) / <c>WAITING...</c> (vàng, khi đang chờ server phản hồi).
/// </summary>
public enum ScanBannerKind
{
    None,
    Waiting,
    Ok,
    Error,
}
