using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventCapacityEntityConfiguration : IEntityTypeConfiguration<TicketedEventCapacity>
{
    public void Configure(EntityTypeBuilder<TicketedEventCapacity> builder)
    {
        builder.ToTable("ticketed_event_capacity");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion<Guid>(v => v.Value, v => new TicketedEventId(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.OwnsMany(e => e.TicketTypeCapacities, b =>
        {
            b.ToJson("ticket_type_capacities");

            b.Property(e => e.Id)
                .HasConversion<Guid>(v => v.Value, v => new TicketTypeId(v));
        });
    }
}
