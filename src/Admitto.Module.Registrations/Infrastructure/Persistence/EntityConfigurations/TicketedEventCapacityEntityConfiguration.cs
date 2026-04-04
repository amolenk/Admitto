using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class EventCapacityEntityConfiguration : IEntityTypeConfiguration<EventCapacity>
{
    public void Configure(EntityTypeBuilder<EventCapacity> builder)
    {
        builder.ToTable("event_capacity");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.OwnsMany(e => e.TicketCapacities, b =>
        {
            b.ToJson("ticket_capacities");

            b.Property(tc => tc.Id)
                .HasJsonPropertyName("slug")
                .IsRequired();

            b.Property(tc => tc.MaxCapacity)
                .HasJsonPropertyName("max_capacity");

            b.Property(tc => tc.UsedCapacity)
                .HasJsonPropertyName("used_capacity")
                .IsRequired();
        });
    }
}
