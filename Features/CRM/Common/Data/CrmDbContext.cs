using CRM.Common.Models;
using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
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
        builder.Entity<Contact>(entity =>
        {
            entity.ToTable("contacts");

            entity.Property(c => c.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.LastName).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Email).HasMaxLength(200);
            entity.Property(c => c.Phone).HasMaxLength(30);
            entity.Property(c => c.JobTitle).HasMaxLength(150);
            entity.Property(c => c.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ContactStatus.Lead);
            entity.Property(c => c.Source)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(ContactSource.Other);
            entity.Property(c => c.AssignedToUserId).HasMaxLength(450);

            entity.HasIndex(c => c.Email).IsUnique().HasFilter("\"email\" IS NOT NULL");
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => c.AssignedToUserId);
            entity.HasIndex(c => c.CompanyId);

            entity.HasOne(c => c.Company)
                .WithMany(co => co.Contacts)
                .HasForeignKey(c => c.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Company>(entity =>
        {
            entity.ToTable("companies");

            entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Industry).HasMaxLength(100);
            entity.Property(c => c.Website).HasMaxLength(300);
            entity.Property(c => c.Phone).HasMaxLength(30);

            entity.HasIndex(c => c.Name);
        });

        builder.Entity<Deal>(entity =>
        {
            entity.ToTable("deals");

            entity.Property(d => d.Title).HasMaxLength(200).IsRequired();
            entity.Property(d => d.Value).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            entity.Property(d => d.Currency).HasMaxLength(10).HasDefaultValue("USD");
            entity.Property(d => d.Stage)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(DealStage.Lead);
            entity.Property(d => d.Probability).HasDefaultValue(10);
            entity.Property(d => d.OwnerUserId).HasMaxLength(450);

            entity.HasIndex(d => d.Stage);
            entity.HasIndex(d => d.OwnerUserId);
            entity.HasIndex(d => d.ContactId);
            entity.HasIndex(d => d.CompanyId);
            entity.HasIndex(d => d.ExpectedCloseDate);

            entity.HasOne(d => d.Contact)
                .WithMany(c => c.Deals)
                .HasForeignKey(d => d.ContactId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Company)
                .WithMany(co => co.Deals)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Activity>(entity =>
        {
            entity.ToTable("activities");

            entity.Property(a => a.Type)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(a => a.Subject).HasMaxLength(300).IsRequired();
            entity.Property(a => a.CreatedByUserId).HasMaxLength(450).IsRequired();
            entity.Property(a => a.IsCompleted).HasDefaultValue(false);

            entity.HasIndex(a => a.ContactId);
            entity.HasIndex(a => a.DealId);
            entity.HasIndex(a => a.CreatedByUserId);
            entity.HasIndex(a => a.IsCompleted);
            entity.HasIndex(a => a.ScheduledAt);

            entity.HasOne(a => a.Contact)
                .WithMany(c => c.Activities)
                .HasForeignKey(a => a.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.Deal)
                .WithMany(d => d.Activities)
                .HasForeignKey(a => a.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Note>(entity =>
        {
            entity.ToTable("notes");

            entity.Property(n => n.Content).IsRequired();
            entity.Property(n => n.CreatedByUserId).HasMaxLength(450).IsRequired();

            entity.HasOne(n => n.Contact)
                .WithMany(c => c.Notes)
                .HasForeignKey(n => n.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Deal)
                .WithMany(d => d.Notes)
                .HasForeignKey(n => n.DealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Tag>(entity =>
        {
            entity.ToTable("tags");

            entity.Property(t => t.Name).HasMaxLength(50).IsRequired();
            entity.Property(t => t.Color).HasMaxLength(7);

            entity.HasIndex(t => t.Name).IsUnique();
        });

        builder.Entity<ContactTag>(entity =>
        {
            entity.ToTable("contact_tags");

            entity.HasKey(ct => new { ct.ContactId, ct.TagId });

            entity.HasOne(ct => ct.Contact)
                .WithMany(c => c.ContactTags)
                .HasForeignKey(ct => ct.ContactId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(ct => ct.Tag)
                .WithMany(t => t.ContactTags)
                .HasForeignKey(ct => ct.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

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
