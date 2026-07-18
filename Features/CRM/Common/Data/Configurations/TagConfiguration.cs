using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> entity)
    {
        entity.ToTable("tags");

        entity.Property(t => t.Name).HasMaxLength(50).IsRequired();
        entity.Property(t => t.Color).HasMaxLength(7);

        entity.HasIndex(t => t.Name).IsUnique();
    }
}
