using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class ContactTagConfiguration : IEntityTypeConfiguration<ContactTag>
{
    public void Configure(EntityTypeBuilder<ContactTag> entity)
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
    }
}
