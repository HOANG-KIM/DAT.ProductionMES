namespace ProductionMES.Infrastructure.Persistence;

/// <summary>
/// Implementation khung của IUnitOfWork, bọc quanh ApplicationDbContext.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private bool _disposed;

    public UnitOfWork(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
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
