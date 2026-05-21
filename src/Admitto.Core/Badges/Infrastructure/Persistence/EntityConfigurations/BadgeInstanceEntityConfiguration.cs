using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.EntityConfigurations;

public class BadgeInstanceEntityConfiguration : IEntityTypeConfiguration<BadgeInstance>
{
    public void Configure(EntityTypeBuilder<BadgeInstance> builder)
    {
        builder.ToTable("badge_instances");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.BadgeTypeId)
            .HasColumnName("badge_type_id")
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .IsRequired()
            .HasMaxLength(BadgeInstanceDisplayName.MaxLength);

        builder.Property(e => e.Notes)
            .HasColumnName("notes")
            .HasMaxLength(BadgeInstanceNotes.MaxLength)
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.HasIndex(e => e.BadgeTypeId)
            .HasDatabaseName("IX_badge_instances_badge_type_id");
    }
}
