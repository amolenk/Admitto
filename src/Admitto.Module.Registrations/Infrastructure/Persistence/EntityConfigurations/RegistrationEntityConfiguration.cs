using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class RegistrationEntityConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => RegistrationId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion<string>(v => v.Value, v => EmailAddress.From(v))
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.HasIndex(e => new { e.EventId, e.Email })
            .HasDatabaseName("IX_registrations_event_id_email")
            .IsUnique();

        builder.OwnsMany(e => e.Tickets, b =>
        {
            b.ToJson("tickets");
            b.Property(t => t.Slug).HasJsonPropertyName("slug").IsRequired();
            b.PrimitiveCollection(t => t.TimeSlots).HasJsonPropertyName("time_slots");
        });

        builder.Property(e => e.AdditionalDetails)
            .HasColumnName("additional_details")
            .HasColumnType("jsonb")
            .HasConversion(AdditionalDetailJsonConverters.DetailsConverter)
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
    }
}
