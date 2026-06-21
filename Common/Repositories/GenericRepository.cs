using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CRM.Common.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly DbContext Context;
    protected readonly DbSet<T> DbSet;

    public GenericRepository(DbContext context)
    {
        Context = context;
        DbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync<TId>(TId id, bool trackChanges = true, CancellationToken ct = default)
        where TId : notnull
    {
        var entity =  await DbSet.FindAsync([id], ct);
        if (entity is not null && !trackChanges)
            Context.Entry(entity).State = EntityState.Detached;
        return entity;
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T,bool>> predicate, bool trackChnages = true
       , CancellationToken ct = default)
    {
        var query = DbSet.Where(predicate);
        if(!trackChnages) query = query.AsNoTracking();
        return await query.ToListAsync(ct);
    }

    public virtual IQueryable<T> Query(bool trackChnages = false) => trackChnages ? DbSet.AsQueryable():DbSet.AsNoTracking();

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AnyAsync(predicate, ct);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null ? await DbSet.CountAsync(ct) : await DbSet.CountAsync(predicate, ct);

    public virtual void Add(T entity) => DbSet.Add(entity);
    public void AddRange(IEnumerable<T> entities) =>  DbSet.AddRange(entities);
 

    public virtual void Update(T entity) => DbSet.Update(entity);

    public virtual void Delete(T entity) => DbSet.Remove(entity);
    public void DeleteRange(IEnumerable<T> entities)=>  DbSet.RemoveRange(entities);
}
