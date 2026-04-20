using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class TicketedEventLifecycleGuardEntityConfiguration
    : IEntityTypeConfiguration<TicketedEventLifecycleGuard>
{
    public void Configure(EntityTypeBuilder<TicketedEventLifecycleGuard> builder)
    {
        builder.ToTable("event_lifecycle_guard");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.LifecycleStatus)
            .HasColumnName("lifecycle_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(EventLifecycleStatus.Active)
            .IsRequired();

        builder.Property(e => e.PolicyMutationCount)
            .HasColumnName("policy_mutation_count")
            .HasDefaultValue(0L)
            .IsRequired();
    }
}
