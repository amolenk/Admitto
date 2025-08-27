using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventEntityConfiguration : IEntityTypeConfiguration<TicketedEvent>
{
    public void Configure(EntityTypeBuilder<TicketedEvent> builder)
    {
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        // TODO Consider using this pattern for all entities
        builder
            .HasOne<Team>() // no navigation
            .WithMany()
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Website)
            .HasColumnName("website")
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.StartTime)
            .HasColumnName("start_time")
            .IsRequired();

        builder.Property(e => e.EndTime)
            .HasColumnName("end_time")
            .IsRequired();

        builder.Property(e => e.RegistrationStartTime)
            .HasColumnName("registration_start_time")
            .IsRequired();

        builder.Property(e => e.RegistrationEndTime)
            .HasColumnName("registration_end_time")
            .IsRequired();

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .IsRequired();
        
        builder.OwnsOne<TicketedEventPolicies>(e => e.ConfiguredPolicies, b =>
            {
                b.ToJson("policies");
                
                b.OwnsOne<CancellationPolicy>(b => b.CancellationPolicy, cpb =>
                {
                    cpb.ToJson();
                });
            });

        builder.OwnsMany(e => e.TicketTypes, b =>
        {
            b.ToJson("ticket_types");
        });

        builder.Property(e => e.Attendees)
            .HasColumnName("attendees")
            .IsRequired();
        
        builder
            .HasIndex(e => new { e.TeamId, e.Slug })
            .IsUnique();
    }
}
