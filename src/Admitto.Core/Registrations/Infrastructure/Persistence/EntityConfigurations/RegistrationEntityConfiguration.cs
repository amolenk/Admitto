using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class RegistrationEntityConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(e => e.Id);

        var idProperty = builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        // Vogen throws on GetHashCode() for uninitialized structs. EF Core calls GetHashCode()
        // on the FK shadow property of owned entities (TicketTypeSnapshot) before FK propagation,
        // so we register a safe comparer here. It propagates from the PK to FK shadow properties.
        idProperty.Metadata.SetValueComparer(new ValueComparer<RegistrationId>(
            (x, y) => x.IsInitialized() == y.IsInitialized() && (!x.IsInitialized() || x.Value == y.Value),
            v => v.IsInitialized() ? v.GetHashCode() : 0,
            v => v));

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.Property(e => e.FirstName)
            .HasColumnName("first_name")
            .IsRequired()
            .HasMaxLength(FirstName.MaxLength);

        builder.Property(e => e.LastName)
            .HasColumnName("last_name")
            .IsRequired()
            .HasMaxLength(LastName.MaxLength);

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(16);

        builder.Property(e => e.HasReconfirmed)
            .HasColumnName("has_reconfirmed")
            .IsRequired();

        builder.Property(e => e.ReconfirmedAt)
            .HasColumnName("reconfirmed_at");

        builder.Property(e => e.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.HasIndex(e => new { e.EventId, e.Email })
            .HasDatabaseName("IX_registrations_event_id_email")
            .IsUnique();

        builder.OwnsMany(e => e.Tickets, (OwnedNavigationBuilder<Registration, TicketTypeSnapshot> b) =>
        {
            b.ToJson("tickets");
            b.Property(t => t.Id)
                .HasJsonPropertyName("id")
                .HasConversion<TicketTypeId.EfCoreValueConverter>()
                .IsRequired();
            b.Property(t => t.Name)
                .HasJsonPropertyName("name")
                .IsRequired();
            b.PrimitiveCollection(t => t.TimeSlots)
                .HasJsonPropertyName("time_slots")
                .ElementType(et => et.HasConversion<TimeSlot.EfCoreValueConverter>());
        });

        builder.Property(e => e.AdditionalDetails)
            .HasColumnName("additional_details")
            .HasColumnType("jsonb")
            .HasConversion(AdditionalDetailJsonConverters.DetailsConverter)
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();
    }
}
