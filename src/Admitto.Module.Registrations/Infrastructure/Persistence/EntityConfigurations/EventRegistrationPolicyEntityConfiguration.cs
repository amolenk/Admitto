using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class EventRegistrationPolicyEntityConfiguration : IEntityTypeConfiguration<EventRegistrationPolicy>
{
    public void Configure(EntityTypeBuilder<EventRegistrationPolicy> builder)
    {
        builder.ToTable("event_registration_policy");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.RegistrationWindowOpensAt)
            .HasColumnName("registration_window_opens_at");

        builder.Property(e => e.RegistrationWindowClosesAt)
            .HasColumnName("registration_window_closes_at");

        builder.Property(e => e.AllowedEmailDomain)
            .HasColumnName("allowed_email_domain")
            .HasMaxLength(255);

        builder.Property(e => e.EventLifecycleStatus)
            .HasColumnName("event_lifecycle_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(EventLifecycleStatus.Active)
            .IsRequired();
    }
}
