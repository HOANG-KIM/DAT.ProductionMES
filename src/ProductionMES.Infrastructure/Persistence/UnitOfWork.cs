using System.Collections.Concurrent;
using ProductionMES.Application.Abstractions.Persistence;
using ProductionMES.Infrastructure.Repositories;

namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// Implementation của IUnitOfWork, bọc quanh ApplicationDbContext.
/// Cache 1 instance Repository generic cho mỗi kiểu entity trong vòng đời của UnitOfWork (scoped theo request).
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ConcurrentDictionary<Type, object> _repositories = new();
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        var repository = _repositories.GetOrAdd(
            typeof(TEntity),
            _ => new Repository<TEntity>(_dbContext));

        return (IRepository<TEntity>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _dbContext.SaveChangesAsync(cancellationToken);

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _dbContext.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
