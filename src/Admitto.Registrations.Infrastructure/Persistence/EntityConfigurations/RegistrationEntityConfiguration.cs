using Amolenk.Admitto.Registrations.Domain.Entities;
using Amolenk.Admitto.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class RegistrationEntityConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => new RegistrationId(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasConversion(v => v.Value, v => new TicketedEventId(v))
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .HasConversion<string>(v => v.Value, v => EmailAddress.From(v))
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);
        
        builder.HasIndex(e => e.Email).IsUnique();
    }
}
