using Amolenk.Admitto.Application.UseCases.Email;
using Amolenk.Admitto.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class EmailMessageEntityConfiguration : IEntityTypeConfiguration<EmailMessage>
{
    public void Configure(EntityTypeBuilder<EmailMessage> builder)
    {
        builder.ToTable("email_messages");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();
        
        builder.Property(e => e.RecipientEmail)
            .HasColumnName("recipient_email")
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .HasConversion(p => p!.Value, p =>  new TicketedEventId(p));

        builder.Property(e => e.TemplateId)
            .HasColumnName("template_id")
            .IsRequired();

        builder.OwnsMany(e => e.TemplateParameters, b =>
        {
            b.ToJson("template_parameters");
        });
        
        builder.Property(e => e.Priority)
            .HasColumnName("priority")
            .IsRequired();
    }
}
