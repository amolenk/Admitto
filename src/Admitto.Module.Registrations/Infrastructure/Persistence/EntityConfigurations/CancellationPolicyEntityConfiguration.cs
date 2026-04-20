using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class CancellationPolicyEntityConfiguration : IEntityTypeConfiguration<CancellationPolicy>
{
    public void Configure(EntityTypeBuilder<CancellationPolicy> builder)
    {
        builder.ToTable("cancellation_policy");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.LateCancellationCutoff)
            .HasColumnName("late_cancellation_cutoff")
            .IsRequired();
    }
}
