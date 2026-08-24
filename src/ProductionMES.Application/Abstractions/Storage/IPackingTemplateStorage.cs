namespace ProductionMES.Application.Abstractions.Storage;

/// <summary>
/// Lưu trữ file mẫu tem in (template .xlsx) theo <c>PackingModelConfig.Id</c> (US-24/FR-24, AC4/AC5) — đặt tại
/// tầng Application (không phải Infrastructure) để Service phụ thuộc vào abstraction này mà không cần reference
/// ngược sang Infrastructure, cùng pattern với <c>IApiKeyGenerator</c>/<c>IJwtTokenGenerator</c>. Implementation
/// cụ thể (filesystem) nằm ở Infrastructure — KHÔNG lưu BLOB trong MySQL (CLAUDE.md), file đặt tên theo Id (KHÔNG
/// dùng trực tiếp chuỗi Model, vì Model là free-text có thể chứa ký tự không hợp lệ cho filesystem).
/// </summary>
public interface IPackingTemplateStorage
{
    /// <summary>Lưu (ghi đè nếu đã có) file mẫu tem cho 1 cấu hình đóng gói (AC4 — thay thế file mẫu cũ).</summary>
    Task SaveAsync(int packingModelConfigId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Mở stream đọc file mẫu tem hiện có — trả <c>null</c> nếu chưa từng tải lên (AC5).</summary>
    Task<Stream?> OpenReadAsync(int packingModelConfigId, CancellationToken cancellationToken = default);

    /// <summary>Xoá file mẫu tem (nếu có) — dùng khi cần dọn dẹp, không có endpoint AC nào gọi trực tiếp hiện tại.</summary>
    Task DeleteAsync(int packingModelConfigId, CancellationToken cancellationToken = default);
}
