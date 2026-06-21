using System.Linq.Expressions;

namespace CRM.Common.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync<TId>(TId id, bool trackChnages = true,CancellationToken ct = default) where TId : notnull;
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T,bool>>predicate, bool trackChnages = true,CancellationToken ct = default);
    IQueryable<T> Query(bool  trackChnages = false);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    void Add(T entity);
    void AddRange(IEnumerable<T> entities);
    void Update(T entity);
    void Delete(T entity);
    void DeleteRange(IEnumerable<T> entities);
}
