using Amolenk.Admitto.Core.Email.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class EmailTemplateEntityConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");
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

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
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
            .HasColumnType("text");

        // Case-insensitive unique name per scope — enforced as a functional index
        // (lower(name)) via raw SQL in the migration.
        // EF is not aware of this index; duplicate detection is done in handlers
        // before insert and surfaced via EmailPostgresExceptionMapping.
    }
}
