using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> entity)
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
    }
}
