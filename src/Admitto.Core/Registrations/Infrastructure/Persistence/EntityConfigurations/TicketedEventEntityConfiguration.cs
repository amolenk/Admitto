using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventEntityConfiguration : IEntityTypeConfiguration<TicketedEvent>
{
    public void Configure(EntityTypeBuilder<TicketedEvent> builder)
    {
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(EventName.MaxLength);

        builder.Property(e => e.WebsiteUrl)
            .HasColumnName("website_url")
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.PublicSlug)
            .HasColumnName("public_slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength)
            .HasDefaultValueSql("'event-' || substr(md5(random()::text), 1, 12)");

        builder.Property(e => e.StartsAt)
            .HasColumnName("starts_at")
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .HasColumnName("ends_at")
            .IsRequired();

        builder.Property(e => e.TimeZone)
            .HasColumnName("time_zone")
            .HasMaxLength(TimeZoneId.MaxLength)
            .HasDefaultValue(TimeZoneId.From("UTC"))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(e => e.Version)
            .HasColumnName("xmin")
            .IsRowVersion();

        builder.OwnsOne(e => e.RegistrationPolicy, p =>
        {
            p.Property(x => x.OpensAt).HasColumnName("registration_policy_opens_at");
            p.Property(x => x.ClosesAt).HasColumnName("registration_policy_closes_at");
            p.Property(x => x.AllowedEmailDomain)
                .HasColumnName("registration_policy_allowed_email_domain")
                .HasMaxLength(253);
        });

        builder.OwnsOne(e => e.ReconfirmPolicy, p =>
        {
            p.Property(x => x.OpensAt).HasColumnName("reconfirm_policy_opens_at");
            p.Property(x => x.ClosesAt).HasColumnName("reconfirm_policy_closes_at");
            p.Property(x => x.MinEmailInterval).HasColumnName("reconfirm_policy_min_email_interval");
            p.Property(x => x.QuietHoursStart)
                .HasColumnName("reconfirm_policy_quiet_hours_start")
                .HasColumnType("time");
            p.Property(x => x.QuietHoursEnd)
                .HasColumnName("reconfirm_policy_quiet_hours_end")
                .HasColumnType("time");
        });

        builder.OwnsOne(e => e.WaitlistPolicy, p =>
        {
            p.Property(x => x.QuietHoursStart)
                .HasColumnName("waitlist_policy_quiet_hours_start")
                .HasColumnType("time")
                .HasDefaultValue(TicketedEventWaitlistPolicy.DefaultQuietHoursStart)
                .IsRequired();

            p.Property(x => x.QuietHoursEnd)
                .HasColumnName("waitlist_policy_quiet_hours_end")
                .HasColumnType("time")
                .HasDefaultValue(TicketedEventWaitlistPolicy.DefaultQuietHoursEnd)
                .IsRequired();
        });
        builder.Navigation(e => e.WaitlistPolicy).IsRequired();

        builder.HasIndex(e => new { e.TeamId, e.Status })
            .HasDatabaseName("IX_ticketed_events_team_id_status");

        builder.HasIndex(e => e.PublicSlug)
            .IsUnique()
            .HasDatabaseName("IX_ticketed_events_public_slug");

        var schemaProperty = builder.Property(e => e.AdditionalDetailSchema)
            .HasColumnName("additional_detail_schema")
            .HasColumnType("jsonb")
            .HasConversion(AdditionalDetailJsonConverters.SchemaConverter)
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        // AdditionalDetailSchema's record-generated Equals compares IReadOnlyList<AdditionalDetailField>
        // by reference, so EF would detect a spurious change on every load-then-save cycle.
        // The comparer does a deep field-by-field comparison instead.
        schemaProperty.Metadata.SetValueComparer(new ValueComparer<AdditionalDetailSchema>(
            (a, b) => (a == null && b == null) ||
                      (a != null && b != null &&
                       a.Fields.Count == b.Fields.Count &&
                       a.Fields.Zip(b.Fields).All(p => p.First.Equals(p.Second))),
            a => a == null ? 0 : a.Fields.Aggregate(0, (h, f) => HashCode.Combine(h, f.GetHashCode())),
            a => a));
    }
}
