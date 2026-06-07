using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmailSettingsEntityConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        builder.ToTable("email_settings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id");

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

        builder.HasIndex(e => e.TeamId)
            .HasDatabaseName("IX_email_settings_team")
            .HasFilter("ticketed_event_id IS NULL")
            .IsUnique();

        builder.HasIndex(e => new { e.TeamId, e.TicketedEventId })
            .HasDatabaseName("IX_email_settings_team_event")
            .HasFilter("ticketed_event_id IS NOT NULL")
            .IsUnique();
    }
}
