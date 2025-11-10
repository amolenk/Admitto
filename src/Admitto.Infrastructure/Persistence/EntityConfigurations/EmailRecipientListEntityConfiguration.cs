using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class EmailRecipientListEntityConfiguration : IEntityTypeConfiguration<EmailRecipientList>
{
    public void Configure(EntityTypeBuilder<EmailRecipientList> builder)
    {
        builder.ToTable("email_recipient_lists");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(100);
    
        builder.OwnsMany(e => e.Recipients, b =>
        {
            b.ToJson("recipients");
            
            b.OwnsMany(e => e.Details);
        });
        
        builder.HasIndex(e => e.Name).IsUnique();
    }
}
