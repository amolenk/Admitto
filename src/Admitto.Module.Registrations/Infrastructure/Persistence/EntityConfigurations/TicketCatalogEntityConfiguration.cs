using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketCatalogEntityConfiguration : IEntityTypeConfiguration<TicketCatalog>
{
    public void Configure(EntityTypeBuilder<TicketCatalog> builder)
    {
        builder.ToTable("ticket_catalog");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.Property(e => e.EventStatus)
            .HasColumnName("event_status")
            .HasConversion<int>()
            .IsRequired()
            .HasDefaultValue(EventLifecycleStatus.Active);

        builder.OwnsMany(e => e.TicketTypes, b =>
        {
            b.ToJson("ticket_types");

            b.Property(tt => tt.Id)
                .HasJsonPropertyName("slug")
                .IsRequired();

            b.Property(tt => tt.Name)
                .HasJsonPropertyName("name")
                .HasConversion(v => v.Value, v => DisplayName.From(v))
                .IsRequired();

            b.Property(tt => tt.MaxCapacity)
                .HasJsonPropertyName("max_capacity");

            b.Property(tt => tt.UsedCapacity)
                .HasJsonPropertyName("used_capacity")
                .IsRequired();

            b.Property(tt => tt.IsCancelled)
                .HasJsonPropertyName("is_cancelled")
                .IsRequired();

            b.Property(tt => tt.SelfServiceEnabled)
                .HasJsonPropertyName("self_service_enabled")
                .IsRequired();

            b.Property(tt => tt.TimeSlotSlugs)
                .HasJsonPropertyName("time_slots");

            b.Ignore(tt => tt.TimeSlots);
        });
    }
}
