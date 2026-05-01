using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventEntityConfiguration : IEntityTypeConfiguration<TicketedEvent>
{
    public void Configure(EntityTypeBuilder<TicketedEvent> builder)
    {
        builder.ToTable("ticketed_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.TeamId)
            .HasColumnName("team_id")
            .HasConversion<Guid>(v => v.Value, v => TeamId.From(v))
            .IsRequired();

        builder.Property(e => e.TeamSlug)
            .HasColumnName("team_slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.Slug)
            .HasColumnName("slug")
            .IsRequired()
            .HasMaxLength(Slug.MaxLength);

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(DisplayName.MaxLength);

        builder.Property(e => e.WebsiteUrl)
            .HasColumnName("website_url")
            .HasConversion(v => v.Value.ToString(), v => AbsoluteUrl.From(v))
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.BaseUrl)
            .HasColumnName("base_url")
            .HasConversion(v => v.Value.ToString(), v => AbsoluteUrl.From(v))
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(e => e.StartsAt)
            .HasColumnName("starts_at")
            .IsRequired();

        builder.Property(e => e.EndsAt)
            .HasColumnName("ends_at")
            .IsRequired();

        builder.Property(e => e.TimeZone)
            .HasColumnName("time_zone")
            .HasConversion(v => v.Value, v => TimeZoneId.From(v))
            .HasMaxLength(TimeZoneId.MaxLength)
            .HasDefaultValue(TimeZoneId.From("UTC"))
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(e => e.SigningKey)
            .HasColumnName("signing_key")
            .IsRequired()
            .HasMaxLength(64);

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

        builder.OwnsOne(e => e.CancellationPolicy, p =>
        {
            p.Property(x => x.LateCancellationCutoff)
                .HasColumnName("cancellation_policy_late_cutoff");
        });

        builder.OwnsOne(e => e.ReconfirmPolicy, p =>
        {
            p.Property(x => x.OpensAt).HasColumnName("reconfirm_policy_opens_at");
            p.Property(x => x.ClosesAt).HasColumnName("reconfirm_policy_closes_at");
            p.Property(x => x.Cadence).HasColumnName("reconfirm_policy_cadence");
        });

        builder.Property(e => e.AdditionalDetailSchema)
            .HasColumnName("additional_detail_schema")
            .HasColumnType("jsonb")
            .HasConversion(AdditionalDetailJsonConverters.SchemaConverter)
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.HasIndex(e => new { e.TeamId, e.Slug })
            .IsUnique();
    }
}
