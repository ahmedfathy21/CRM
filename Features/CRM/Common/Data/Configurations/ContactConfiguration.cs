using CRM.Features.CRM.Common.Models;
using CRM.Features.CRM.Common.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> entity)
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
    }
}
