using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> entity)
    {
        entity.ToTable("companies");

        entity.Property(c => c.Name).HasMaxLength(200).IsRequired();
        entity.Property(c => c.Industry).HasMaxLength(100);
        entity.Property(c => c.Website).HasMaxLength(300);
        entity.Property(c => c.Phone).HasMaxLength(30);

        entity.HasIndex(c => c.Name);
    }
}
