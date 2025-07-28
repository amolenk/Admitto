using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class EmailTemplateEntityConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(e => e.Subject)
            .HasColumnName("subject")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Body)
            .HasColumnName("body")
            .IsRequired();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id");
        
        builder
            .HasIndex(e => new { e.TeamId, e.TicketedEventId, TemplateType = e.Type })
            .IsUnique();
    }
}
