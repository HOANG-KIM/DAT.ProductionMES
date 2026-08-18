namespace ProductionMES.Domain.Enums;

/// <summary>
/// Kết quả 1 lượt scan (FR-08/FR-10, US-07/US-08/US-18). Thiết kế mở rộng được.
/// </summary>
public enum ScanResult
{
    /// <summary>Lượt scan hợp lệ — qua đủ 2 bước kiểm tra (chống trùng tem + đã qua công đoạn liền trước).</summary>
    Ok = 0,

    /// <summary>Bị từ chối: tem đã được scan OK tại cùng công đoạn này ở bất kỳ Line nào (toàn hệ thống).</summary>
    DuplicateTag = 1,

    /// <summary>Bị từ chối: tem chưa từng scan OK tại công đoạn liền trước ở bất kỳ Line nào (toàn hệ thống).</summary>
    PreviousStageNotPassed = 2,

    /// <summary>
    /// US-18 (FR-18, mục 6 quy tắc 9): sản phẩm không đạt chất lượng, xác nhận thủ công bởi người vận hành ở
    /// Chế độ Scan NG (khác <see cref="DuplicateTag"/>/<see cref="PreviousStageNotPassed"/> — 2 giá trị đó là lỗi
    /// thao tác/quy trình do hệ thống tự động phát hiện, không phải lỗi chất lượng sản phẩm). Tem có bản ghi
    /// <c>Ng</c> tại 1 công đoạn mà CHƯA có bản ghi <see cref="Ok"/> tương ứng sẽ tự động bị chặn khi cố scan sang
    /// công đoạn kế tiếp (do check "đã qua công đoạn liền trước" chỉ thỏa với <see cref="Ok"/>) — không cần thêm
    /// state "khóa" riêng ở entity <c>Scan</c>. Cần "Mở khóa rework" (US-19) để cho phép scan OK lại.
    /// </summary>
    Ng = 3,

    /// <summary>
    /// US-19 AC1 (FR-19): bị từ chối vì tem đang bị khóa rework tại đúng công đoạn này (bản ghi <see cref="Ng"/>
    /// gần nhất tại (TagCode, StageId) CHƯA được Tổ trưởng "Mở khóa rework" — xem
    /// <c>ProductionMES.Application.Services.ReworkUnlocks.ReworkLockCalculator</c>). Khác <see cref="Ng"/>: đây
    /// KHÔNG phải lượt xác nhận lỗi chất lượng mới, mà là hệ thống TỰ ĐỘNG từ chối 1 lượt scan bình thường (qua
    /// <c>ScanService.CreateAsync</c>) vì tem chưa được phép scan lại — cùng nhóm "lỗi thao tác/quy trình" với
    /// <see cref="DuplicateTag"/>/<see cref="PreviousStageNotPassed"/> (KHÔNG tính vào NG/%NG chất lượng ở Andon
    /// board, xem <c>AndonBoardService.NgCount</c> chỉ đếm <see cref="Ng"/>).
    /// </summary>
    WaitingReworkUnlock = 4,
}
