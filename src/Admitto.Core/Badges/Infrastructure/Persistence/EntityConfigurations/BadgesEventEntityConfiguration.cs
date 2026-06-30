using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence.EntityConfigurations;

public class BadgesEventEntityConfiguration : IEntityTypeConfiguration<BadgeEvent>
{
    public void Configure(EntityTypeBuilder<BadgeEvent> builder)
    {
        builder.ToTable("badges_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        // Configure badge types as JSON column owned collection
        builder.OwnsMany(
            e => e.BadgeTypes,
            b =>
            {
                b.ToJson("badge_types");

                b.Property(bt => bt.Id)
                    .HasConversion<BadgeTypeId.EfCoreValueConverter>()
                    .HasJsonPropertyName("id");

                b.Property(bt => bt.Name)
                    .HasConversion<BadgeTypeName.EfCoreValueConverter>()
                    .HasJsonPropertyName("name");

                b.Property(bt => bt.Kind)
                    .HasConversion<string>()
                    .HasJsonPropertyName("kind");

                b.PrimitiveCollection(bt => bt.TicketTypeIds)
                    .ElementType(et => et.HasConversion<TicketTypeId.EfCoreValueConverter>())
                    .HasJsonPropertyName("ticket_type_ids");
            });
    }
}

