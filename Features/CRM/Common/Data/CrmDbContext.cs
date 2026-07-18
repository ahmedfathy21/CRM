using CRM.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Common.Data;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    public DbSet<Models.Contact> Contacts => Set<Models.Contact>();
    public DbSet<Models.Company> Companies => Set<Models.Company>();
    public DbSet<Models.Deal> Deals => Set<Models.Deal>();
    public DbSet<Models.Activity> Activities => Set<Models.Activity>();
    public DbSet<Models.Note> Notes => Set<Models.Note>();
    public DbSet<Models.Tag> Tags => Set<Models.Tag>();
    public DbSet<Models.ContactTag> ContactTags => Set<Models.ContactTag>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(typeof(CrmDbContext).Assembly);

        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType).Property(nameof(BaseEntity.CreatedAt))
                    .HasDefaultValueSql("NOW()");

                builder.Entity(entityType.ClrType).Property(nameof(BaseEntity.UpdatedAt))
                    .HasDefaultValueSql("NOW()");
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTime.UtcNow;
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTime.UtcNow;
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }
}
