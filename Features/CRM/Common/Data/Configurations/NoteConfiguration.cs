using CRM.Features.CRM.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CRM.Features.CRM.Common.Data.Configurations;

public class NoteConfiguration : IEntityTypeConfiguration<Note>
{
    public void Configure(EntityTypeBuilder<Note> entity)
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
    }
}
