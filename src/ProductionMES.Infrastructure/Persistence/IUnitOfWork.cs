namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// Khung Unit of Work — điều phối lưu thay đổi qua nhiều repository trong 1 transaction logic.
/// Sẽ bổ sung các property repository cụ thể (vd IScanHistoryRepository...) khi có thiết kế entity chi tiết.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
