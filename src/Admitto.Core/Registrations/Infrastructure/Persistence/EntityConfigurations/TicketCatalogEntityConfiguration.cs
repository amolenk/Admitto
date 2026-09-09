using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketCatalogEntityConfiguration : IEntityTypeConfiguration<TicketCatalog>
{
    public void Configure(EntityTypeBuilder<TicketCatalog> builder)
    {
        builder.ToTable("ticket_catalog");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.Property(e => e.EventStatus)
            .HasColumnName("event_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue(EventLifecycleStatus.Active);

        builder.OwnsMany(e => e.TicketTypes, (OwnedNavigationBuilder<TicketCatalog, TicketType> b) =>
        {
            b.ToJson("ticket_types");

            b.Property(tt => tt.Id)
                .HasJsonPropertyName("id")
                .HasConversion<TicketTypeId.EfCoreValueConverter>()
                .IsRequired();

            b.Property(tt => tt.Name)
                .HasJsonPropertyName("name")
                .IsRequired();

            b.Property(tt => tt.MaxCapacity)
                .HasJsonPropertyName("max_capacity");

            b.Property(tt => tt.UsedCapacity)
                .HasJsonPropertyName("used_capacity")
                .IsRequired();

            b.Property(tt => tt.SelfServiceEnabled)
                .HasJsonPropertyName("self_service_enabled")
                .IsRequired();

            b.Property(tt => tt.WaitlistEnabled)
                .HasJsonPropertyName("waitlist_enabled")
                .IsRequired();

            b.Property(tt => tt.WaitlistMode)
                .HasJsonPropertyName("waitlist_mode")
                .IsRequired();

            b.Property(tt => tt.ClaimWindowHours)
                .HasJsonPropertyName("claim_window_hours")
                .IsRequired();

            b.Property(tt => tt.MaxReconfirmationEmails)
                .HasJsonPropertyName("max_reconfirm_attempts");

            b.PrimitiveCollection(tt => tt.TimeSlots)
                .HasJsonPropertyName("time_slots")
                .ElementType(et => et.HasConversion<TimeSlot.EfCoreValueConverter>());
        });
    }
}
