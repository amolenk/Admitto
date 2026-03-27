// using Amolenk.Admitto.Module.Registrations.Domain.Entities;
// using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
// using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
//
// namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;
//
// public class TicketedEventCapacityEntityConfiguration : IEntityTypeConfiguration<EventCapacity>
// {
//     public void Configure(EntityTypeBuilder<EventCapacity> builder)
//     {
//         builder.ToTable("ticketed_event_capacity");
//         builder.HasKey(e => e.Id);
//         
//         builder.Property(e => e.Id)
//             .HasColumnName("id")
//             .HasConversion<Guid>(v => v.Value, v => new TicketedEventId(v))
//             .IsRequired()
//             .ValueGeneratedNever();
//
//         builder.OwnsMany(e => e.TicketCapacities, b =>
//         {
//             b.ToJson("ticket_type_capacities");
//
//             b.Property(e => e.Id)
//                 .HasConversion<Guid>(v => v.Value, v => new TicketTypeId(v));
//         });
//     }
// }
