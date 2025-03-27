// using Amolenk.Admitto.Domain.Entities;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.EntityFrameworkCore.Metadata.Builders;
//
// namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;
//
// public class TicketedEventsEntityConfiguration : IEntityTypeConfiguration<TicketedEvent>
// {
//     public void Configure(EntityTypeBuilder<TicketedEvent> builder)
//     {
//         builder.ToTable("ticketed_events");
//         builder.HasKey(e => e.Id);
//         
//         builder.Property(e => e.Id)
//             .HasColumnName("id")
//             .ValueGeneratedNever();
//         
//         builder.Property(e => e.Name)
//             .HasColumnName("name")
//             .IsRequired()
//             .HasMaxLength(100);
//         
//         builder.Property(e => e.StartDay)
//             .HasColumnName("start_day")
//             .IsRequired();
//         
//         builder.Property(e => e.EndDay)
//             .HasColumnName("end_day")
//             .IsRequired();
//         
//         builder.Property(e => e.SalesStartDateTime)
//             .HasColumnName("sales_start_date_time")
//             .IsRequired();
//         
//         builder.Property(e => e.SalesEndDateTime)
//             .HasColumnName("sales_end_date_time")
//             .IsRequired();
//         
//         builder
//             .OwnsMany(e => e.TicketTypes, b =>
//             {
//                 b.ToJson("ticket_types");
//             });
//     }
// }
