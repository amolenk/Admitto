using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class OtpCodeEntityConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.ToTable("otp_codes");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
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

        builder.HasIndex(e => new { e.EmailHash, e.EventId });
    }
}
