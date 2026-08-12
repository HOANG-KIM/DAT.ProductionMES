using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Infrastructure.Persistence;

namespace ProductionMES.Infrastructure.Repositories;

/// <summary>
/// Implementation generic của <see cref="IRepository{TEntity}"/> dựa trên EF Core, dùng chung cho các entity
/// chỉ cần CRUD đơn giản (vd. Line). Entity nào cần truy vấn phức tạp riêng sẽ có repository chuyên biệt bổ sung sau.
/// </summary>
/// <typeparam name="TEntity">Kiểu entity quản lý bởi repository.</typeparam>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _dbSet;

    public Repository(ApplicationDbContext dbContext)
    {
        _dbSet = dbContext.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(object id, CancellationToken cancellationToken = default)
        => await _dbSet.FindAsync(new[] { id }, cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(Func<IQueryable<TEntity>, IQueryable<TEntity>> queryModifier, CancellationToken cancellationToken = default)
        => await queryModifier(_dbSet).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.Where(predicate).ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await _dbSet.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => _dbSet.Update(entity);

    public void Remove(TEntity entity) => _dbSet.Remove(entity);
}
