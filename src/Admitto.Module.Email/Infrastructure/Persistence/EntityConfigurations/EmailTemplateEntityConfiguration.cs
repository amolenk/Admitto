using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmailTemplateEntityConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion(v => v.Value, v => EmailTemplateId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Scope)
            .HasColumnName("scope")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.ScopeId)
            .HasColumnName("scope_id")
            .IsRequired();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.TextBody)
            .HasColumnName("text_body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.HtmlBody)
            .HasColumnName("html_body")
            .HasColumnType("text")
            .IsRequired();

        builder.HasIndex(e => new { e.Scope, e.ScopeId, e.Type })
            .HasDatabaseName("IX_email_templates_scope_scope_id_type")
            .IsUnique();
    }
}
