using ProductionMES.Application.DTOs.PackingBoxes;
using ProductionMES.Domain.Entities;

namespace ProductionMES.Application.Services.PackingBoxes;

/// <summary>
/// Quản lý thùng tại công đoạn "Đóng thùng" (US-25/FR-25) — đếm số lượng theo Quy cách đóng gói (US-24), snapshot
/// bất biến khi mở thùng (AC12), tự động hoàn tất/mở thùng kế tiếp (AC4), sửa số thùng (AC7), audit tem trùng
/// (AC8).
/// </summary>
public interface IPackingBoxService
{
    /// <summary>AC5/AC6/AC9: trạng thái đóng thùng hiện tại của trạm — Station.Wpf gọi khi khởi động/quay lại màn hình.</summary>
    Task<PackingBoxStateDto> GetStateAsync(int workStationId, CancellationToken cancellationToken = default);

    /// <summary>AC5: nhập số thùng bắt đầu — chỉ cho phép khi kế hoạch hiện tại CHƯA từng có thùng nào tại công đoạn này.</summary>
    Task<PackingBoxDto> SetStartingBoxNoAsync(int workStationId, int startingBoxNo, CancellationToken cancellationToken = default);

    /// <summary>AC7: sửa số thùng hiện tại (đã xác thực Supervisor ở Controller) — không đổi ScannedQuantity/TargetQuantity.</summary>
    Task<PackingBoxDto> UpdateCurrentBoxNoAsync(
        int workStationId, int newBoxNo, int updatedByUserId, string updatedByUserName, CancellationToken cancellationToken = default);

    /// <summary>AC8: xác nhận đã biết tình huống tem trùng — CHỈ audit, không cộng số lượng/không tạo bản ghi Scan mới.</summary>
    Task<PackingDuplicateConfirmationDto> ConfirmDuplicateAsync(
        int workStationId, string tagCode, int confirmedByUserId, string confirmedByUserName, string? note, CancellationToken cancellationToken = default);

    /// <summary>Tải file tem thùng đã merge dữ liệu từ mẫu tem (template) của Model — dùng cho in tự động (AC4) lẫn In lại thủ công (AC13).</summary>
    Task<(byte[] Content, string FileName)> GenerateLabelAsync(int boxId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dùng NỘI BỘ bởi <c>ScanService.CreateAsync</c> TRƯỚC khi chạy 2 bước kiểm tra FR-08 — trả về thùng
    /// InProgress hiện tại nếu đủ điều kiện quét; ném <see cref="Domain.Exceptions.BusinessRuleException"/> nếu
    /// Model chưa có Quy cách đóng gói (AC11) hoặc chưa nhập số thùng bắt đầu (AC5) — KHÔNG lưu bản ghi Scan nào
    /// cho 2 trường hợp này (cùng nguyên tắc lỗi cấu hình/vận hành như "không có kế hoạch Running").
    /// </summary>
    Task<PackingBox> EnsureReadyForScanAsync(WorkStation workStation, ProductionPlan productionPlan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Dùng NỘI BỘ bởi <c>ScanService.CreateAsync</c> NGAY SAU KHI lưu 1 bản ghi Scan Ok tại "Đóng thùng" — tăng
    /// <see cref="PackingBox.ScannedQuantity"/>, tự động hoàn tất + mở thùng kế tiếp khi đạt đủ (AC2/AC4/AC12).
    /// </summary>
    Task<PackingScanOutcome> RegisterOkScanAsync(PackingBox currentBox, CancellationToken cancellationToken = default);
}
