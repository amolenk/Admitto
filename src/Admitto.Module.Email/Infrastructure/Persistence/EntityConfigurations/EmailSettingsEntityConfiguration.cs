using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmailSettingsEntityConfiguration : IEntityTypeConfiguration<EmailSettings>
{
    public void Configure(EntityTypeBuilder<EmailSettings> builder)
    {
        builder.ToTable("email_settings");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EmailSettingsId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Scope)
            .HasColumnName("scope")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ScopeId)
            .HasColumnName("scope_id")
            .IsRequired();

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

        builder.HasIndex(e => new { e.Scope, e.ScopeId })
            .HasDatabaseName("IX_email_settings_scope_scope_id")
            .IsUnique();
    }
}
