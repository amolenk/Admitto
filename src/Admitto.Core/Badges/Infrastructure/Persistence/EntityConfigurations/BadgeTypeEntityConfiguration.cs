using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Badges.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.EntityConfigurations;

public class BadgeTypeEntityConfiguration : IEntityTypeConfiguration<BadgeType>
{
    public void Configure(EntityTypeBuilder<BadgeType> builder)
    {
        builder.ToTable("badge_types");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(BadgeTypeName.MaxLength);

        builder.Property(e => e.Kind)
            .HasColumnName("kind")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.PrimitiveCollection(e => e.TicketTypeIds)
            .HasColumnName("ticket_type_ids")
            .HasColumnType("jsonb")
            .ElementType(et => et.HasConversion<TicketTypeId.EfCoreValueConverter>());

        builder.HasIndex(e => new { e.EventId, e.Name })
            .HasDatabaseName("IX_badge_types_event_id_name")
            .IsUnique();
    }
}
