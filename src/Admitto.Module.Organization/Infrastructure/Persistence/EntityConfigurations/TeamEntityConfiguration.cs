using Amolenk.Admitto.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Organization.Infrastructure.Persistence.EntityConfigurations;

public class TeamEntityConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(DisplayName.MaxLength);

        builder.Property(e => e.EmailAddress)
            .HasColumnName("email_address")
            .IsRequired()
            .HasMaxLength(EmailAddress.MaxLength);

        builder.Property(e => e.ArchivedAt)
            .HasColumnName("archived_at");

        builder.Property(e => e.ActiveEventCount)
            .HasColumnName("active_event_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CancelledEventCount)
            .HasColumnName("cancelled_event_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.ArchivedEventCount)
            .HasColumnName("archived_event_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.PendingEventCount)
            .HasColumnName("pending_event_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.OwnsMany(
            e => e.EventCreationRequests,
            req =>
            {
                req.ToTable("team_event_creation_requests");
                req.WithOwner().HasForeignKey("team_id");
                req.HasKey(r => r.Id);

                req.Property(r => r.Id)
                    .HasColumnName("id")
                    .HasConversion(v => v.Value, v => CreationRequestId.From(v))
                    .IsRequired()
                    .ValueGeneratedNever();

                req.Property<TeamId>("team_id")
                    .HasColumnName("team_id");

                req.Property(r => r.RequestedSlug)
                    .HasColumnName("requested_slug")
                    .IsRequired()
                    .HasMaxLength(Slug.MaxLength);

                req.Property(r => r.RequesterId)
                    .HasColumnName("requester_id")
                    .IsRequired();

                req.Property(r => r.RequestedAt)
                    .HasColumnName("requested_at")
                    .IsRequired();

                req.Property(r => r.Status)
                    .HasColumnName("status")
                    .HasConversion<int>()
                    .IsRequired();

                req.Property(r => r.TicketedEventId)
                    .HasColumnName("ticketed_event_id")
                    .HasConversion(
                        v => v!.Value.Value,
                        v => TicketedEventId.From(v));

                req.Property(r => r.RejectionReason)
                    .HasColumnName("rejection_reason")
                    .HasMaxLength(200);

                req.Property(r => r.CompletedAt)
                    .HasColumnName("completed_at");

                req.Property(r => r.ObservedEventStatus)
                    .HasColumnName("observed_event_status")
                    .HasConversion<int?>();

                req.HasIndex("team_id", nameof(TeamEventCreationRequest.Status));
            });

        builder.HasIndex(e => e.Slug)
            .IsUnique();
    }
}
