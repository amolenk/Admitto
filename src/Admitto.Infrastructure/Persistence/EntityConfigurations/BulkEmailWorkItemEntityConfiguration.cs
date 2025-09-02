using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class BulkEmailWorkItemEntityConfiguration : IEntityTypeConfiguration<BulkEmailWorkItem>
{
    public void Configure(EntityTypeBuilder<BulkEmailWorkItem> builder)
    {
        builder.ToTable("bulk_email_work_items");
        builder.HasKey(e => e.Id);
        
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("event_id");

        builder.Property(e => e.EmailType)
            .HasColumnName("email_type")
            .IsRequired()
            .HasMaxLength(ColumnMaxLength.EmailType);

        builder.Property(e => e.EarliestSendTime)
            .HasColumnName("earliest_send_time")
            .IsRequired();

        builder.Property(e => e.LatestSendTime)
            .HasColumnName("latest_send_time")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
    }
}
