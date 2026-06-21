using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CRM.Common.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DbContext _context;
    private readonly Dictionary<Type, object> _repositories = [];

    public UnitOfWork(DbContext context)
    {
        _context = context;
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
            _repositories[type] = new GenericRepository<T>(_context);
        return (IGenericRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        await _context.SaveChangesAsync(ct);

    public async Task BeginTransactionAsync(CancellationToken ct = default) =>
        await _context.Database.BeginTransactionAsync(ct);

    public async Task RollBackTransactionAsync(CancellationToken ct = default) =>await _context.Database.RollbackTransactionAsync(ct);
    

    public async Task CommitTransactionAsync(CancellationToken ct = default) => await  _context.Database.CommitTransactionAsync(ct); 

    public void Dispose() => _context.Dispose();
}
