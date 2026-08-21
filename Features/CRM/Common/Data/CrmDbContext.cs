using CRM.Common.Models;
using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace CRM.Features.CRM.Common.Data;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options)
    {
    }

    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Deal> Deals => Set<Deal>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Note> Notes => Set<Note>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<ContactTag> ContactTags => Set<ContactTag>();

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
        
        builder.Entity<Activity>().HasQueryFilter(a => !a.IsDeleted);

        // Dashboard Optimization Indexes
        builder.Entity<Deal>().HasIndex(d => new { d.OwnerUserId, d.Stage });
        builder.Entity<Deal>().HasIndex(d => new { d.ClosedAt });
        
        builder.Entity<Contact>().HasIndex(c => new { c.AssignedToUserId, c.Status });
        
        builder.Entity<Activity>().HasIndex(a => new { a.CreatedByUserId, a.IsCompleted, a.ScheduledAt });
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
