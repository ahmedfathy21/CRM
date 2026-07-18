using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> entity)
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
    }
}
