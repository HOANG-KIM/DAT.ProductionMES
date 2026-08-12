namespace ProductionMES.Domain.Exceptions;

/// <summary>
/// Business exception dùng chung khi Service không tìm thấy entity theo Id để cập nhật/thao tác.
/// Được middleware xử lý exception toàn cục tại tầng Api ánh xạ sang HTTP 404.
/// </summary>
public class EntityNotFoundException : Exception
{
    public EntityNotFoundException(string message) : base(message)
    {
    }
}
