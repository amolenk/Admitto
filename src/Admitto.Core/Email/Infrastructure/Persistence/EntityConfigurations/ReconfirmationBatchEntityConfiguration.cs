using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ReconfirmationBatchEntityConfiguration
    : IEntityTypeConfiguration<ReconfirmationBatch>
{
    public void Configure(EntityTypeBuilder<ReconfirmationBatch> builder)
    {
        builder.ToTable("reconfirmation_batches");
        builder.HasKey(batch => batch.Id);

        builder.Property(batch => batch.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(batch => batch.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(batch => batch.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .IsRequired();

        builder.Property(batch => batch.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(batch => batch.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(batch => batch.StartedAt)
            .HasColumnName("started_at")
            .HasColumnType("timestamptz");

        builder.Property(batch => batch.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamptz");

        builder.Property(batch => batch.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        builder.HasIndex(batch => new { batch.TicketedEventId, batch.CreatedAt })
            .HasDatabaseName("IX_reconfirmation_batches_event_created_at")
            .IsDescending(false, true);

        builder.HasIndex(batch => batch.TicketedEventId)
            .HasDatabaseName("IX_reconfirmation_batches_active_event")
            .IsUnique()
            .HasFilter("status IN ('Pending', 'Sending')");
    }
}
