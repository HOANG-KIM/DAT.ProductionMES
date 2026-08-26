namespace ProductionMES.Station.Wpf.ViewModels;

/// <summary>
/// Trạng thái hiện tại của banner kết quả scan trên <c>AndonBoardWindow</c> (US-07 AC3/AC4/AC5) — quyết định
/// tiêu đề/màu/hành vi đóng, theo mockup đã chốt (memory <c>station-wpf-dashboard-mockup</c>): <c>OK INPUT</c>
/// (xanh, tự đóng) / <c>NG INPUT</c> (đỏ, chờ xác nhận) / <c>WAITING...</c> (vàng, khi đang chờ server phản hồi).
/// </summary>
/// <remarks>
/// US-27 (25/08/2026): <see cref="Error"/> nay dùng cho 2 tình huống khác nhau, phân biệt bởi
/// <c>AndonBoardViewModel.RequiresRejectDecision</c> (banner Lưu/Thoát AC3, scan bị hệ thống tự động từ chối) vs
/// <c>AndonBoardViewModel.RequiresAcknowledgement</c> (banner "NG đã ghi nhận" của US-18, giữ nguyên 1 nút "Đã
/// đọc, đóng" — AC2 không đổi). <see cref="Saved"/> mới: banner "Lưu thành công" (AC6) sau khi Tổ trưởng xác nhận
/// lưu 1 lượt scan bị từ chối — cùng màu xanh + cùng cơ chế tự đóng 1.5s với <see cref="Ok"/>.
/// </remarks>
public enum ScanBannerKind
{
    None,
    Waiting,
    Ok,
    Error,
    Saved,
}
