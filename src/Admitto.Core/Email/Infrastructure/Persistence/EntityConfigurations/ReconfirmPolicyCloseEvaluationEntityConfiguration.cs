using Amolenk.Admitto.Core.Email.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence.EntityConfigurations;

internal sealed class ReconfirmPolicyCloseEvaluationEntityConfiguration
    : IEntityTypeConfiguration<ReconfirmPolicyCloseEvaluation>
{
    public void Configure(EntityTypeBuilder<ReconfirmPolicyCloseEvaluation> builder)
    {
        builder.ToTable("reconfirm_policy_close_evaluations");
        builder.HasKey(e => new { e.TeamId, e.TicketedEventId, e.ClosesAt });

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.TicketedEventId)
            .HasColumnName("ticketed_event_id")
            .IsRequired();

        builder.Property(e => e.ClosesAt)
            .HasColumnName("closes_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.EvaluatedAt)
            .HasColumnName("evaluated_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.HasIndex(e => new { e.TicketedEventId, e.ClosesAt })
            .HasDatabaseName("IX_reconfirm_policy_close_evaluations_event_close");
    }
}
