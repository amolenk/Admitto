using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class OtpCodeEntityConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => OtpCodeId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .HasConversion(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .HasConversion(v => v.Value, v => TeamId.From(v))
            .IsRequired();

        builder.Property(e => e.EmailHash)
            .HasColumnName("email_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.CodeHash)
            .HasColumnName("code_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(e => e.UsedAt)
            .HasColumnName("used_at");

        builder.Property(e => e.FailedAttempts)
            .HasColumnName("failed_attempts")
            .IsRequired();

        builder.Property(e => e.SupersededAt)
            .HasColumnName("superseded_at");

        builder.Property(e => e.Version)
            .IsRowVersion();

        builder.HasIndex(e => new { e.EmailHash, e.EventId });
    }
}
