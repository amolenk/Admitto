using Amolenk.Admitto.Application.Common.Email.Verification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class EmailVerificationRequestEntityConfiguration : IEntityTypeConfiguration<EmailVerificationRequest>
{
    public void Configure(EntityTypeBuilder<EmailVerificationRequest> builder)
    {
        builder.ToTable("email_verification_requests");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailAddress);

        builder.Property(e => e.HashedCode)
            .HasColumnName("code")
            .HasColumnType("text") // Use text to allow larger encrypted strings
            .IsRequired();

        builder.Property(e => e.RequestedAt)
            .HasColumnName("requested_at")
            .IsRequired();

        builder.Property(e => e.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();
    }
}
