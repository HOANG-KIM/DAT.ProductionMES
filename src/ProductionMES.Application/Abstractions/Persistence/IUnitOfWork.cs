namespace ProductionMES.Application.Abstractions.Persistence;

/// <summary>
/// Unit of Work — điều phối lưu thay đổi qua nhiều repository trong 1 transaction logic.
/// <see cref="Repository{TEntity}"/> trả về repository generic dùng chung cho các entity CRUD đơn giản;
/// entity nào cần truy vấn phức tạp riêng sẽ có interface repository chuyên biệt bổ sung sau (không thay thế UoW này).
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
