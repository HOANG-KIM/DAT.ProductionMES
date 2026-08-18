using ProductionMES.Application.DTOs.ReworkUnlocks;

namespace ProductionMES.Application.Services.ReworkUnlocks;

public interface IReworkUnlockService
{
    /// <summary>
    /// US-19 AC2/AC6: "Mở khóa rework" cho 1 tem đang bị khóa (bản ghi <see cref="Domain.Enums.ScanResult.Ng"/>
    /// gần nhất tại (TagCode, StageId) chưa được mở khóa — xem <see cref="ReworkLockCalculator"/>). Ném
    /// <see cref="Domain.Exceptions.BusinessRuleException"/> nếu tem hiện KHÔNG bị khóa (vd chưa từng Ng, hoặc đã
    /// mở khóa rồi) — không tạo bản ghi <see cref="Domain.Entities.ReworkUnlock"/> nào trong trường hợp đó.
    /// </summary>
    /// <param name="workStationId">
    /// Id trạm nơi Tổ trưởng đang đứng để mở khóa — dùng để suy ra <c>StageId</c> (Tổ trưởng thao tác tại đúng
    /// công đoạn của trạm, không chọn Công đoạn riêng trên UI — xem <see cref="DTOs.ReworkUnlocks.ReworkUnlockRequest"/>).
    /// </param>
    /// <param name="tagCode">Mã tem cần mở khóa.</param>
    /// <param name="note">Ghi chú tùy chọn (AC2).</param>
    /// <param name="unlockedByUserId">Id tài khoản Tổ trưởng/Admin thực hiện — lấy từ claim Bearer token đã xác thực, không tin theo request body.</param>
    /// <param name="unlockedByUserName">Tên đăng nhập của <paramref name="unlockedByUserId"/> — cũng lấy từ claim.</param>
    Task<ReworkUnlockDto> UnlockAsync(
        int workStationId, string tagCode, string? note, int unlockedByUserId, string unlockedByUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tra cứu thông tin lỗi NG gần nhất + trạng thái khóa rework hiện tại của <paramref name="tagCode"/> tại đúng
    /// công đoạn của <paramref name="workStationId"/> — phục vụ hiển thị tham khảo trên màn "Mở khóa rework" TRƯỚC
    /// khi Tổ trưởng bấm xác nhận (feedback 18/08/2026), KHÔNG chặn <see cref="UnlockAsync"/>. Không ném lỗi khi
    /// tem chưa từng NG — trả về DTO với <see cref="ReworkLockStatusDto.HasNgHistory"/> = false.
    /// </summary>
    /// <param name="workStationId">Id trạm nơi Tổ trưởng đang đứng — dùng để suy ra <c>StageId</c>, cùng cách <see cref="UnlockAsync"/>.</param>
    /// <param name="tagCode">Mã tem cần tra cứu.</param>
    Task<ReworkLockStatusDto> GetLockStatusAsync(int workStationId, string tagCode, CancellationToken cancellationToken = default);
}
