using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EventEmailSettingsEntityConfiguration : IEntityTypeConfiguration<EventEmailSettings>
{
    public void Configure(EntityTypeBuilder<EventEmailSettings> builder)
    {
        builder.ToTable("event_email_settings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("ticketed_event_id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.SmtpHost)
            .HasColumnName("smtp_host")
            .HasMaxLength(Hostname.MaxLength)
            .IsRequired();

        builder.Property(e => e.SmtpPort)
            .HasColumnName("smtp_port")
            .IsRequired();

        builder.Property(e => e.FromAddress)
            .HasColumnName("from_address")
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();

        builder.Property(e => e.AuthMode)
            .HasColumnName("auth_mode")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.Username)
            .HasColumnName("username")
            .HasMaxLength(SmtpUsername.MaxLength);

        builder.Property(e => e.ProtectedPassword)
            .HasColumnName("protected_password");
    }
}
