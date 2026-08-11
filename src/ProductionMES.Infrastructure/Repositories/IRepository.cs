using System.Linq.Expressions;

namespace ProductionMES.Infrastructure.Repositories;

/// <summary>
/// Interface repository generic làm mẫu cho pattern Repository + Unit of Work.
/// Các repository cụ thể theo từng entity sẽ kế thừa/implement khi có thiết kế entity chi tiết.
/// </summary>
/// <typeparam name="TEntity">Kiểu entity quản lý bởi repository.</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    void Update(TEntity entity);

    void Remove(TEntity entity);
}
