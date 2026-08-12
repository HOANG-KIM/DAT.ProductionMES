using System.Linq.Expressions;

namespace ProductionMES.Application.Abstractions.Persistence;

/// <summary>
/// Interface repository generic làm mẫu cho pattern Repository + Unit of Work.
/// Đặt tại tầng Application (không phải Infrastructure) để Service có thể phụ thuộc vào abstraction này
/// mà không cần reference ngược sang Infrastructure — implementation cụ thể (EF Core) nằm ở Infrastructure.
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
