using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence.EntityConfigurations;

public class ReconfirmPolicyEntityConfiguration : IEntityTypeConfiguration<ReconfirmPolicy>
{
    public void Configure(EntityTypeBuilder<ReconfirmPolicy> builder)
    {
        builder.ToTable("reconfirm_policy");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("event_id")
            .HasConversion<Guid>(v => v.Value, v => TicketedEventId.From(v))
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(e => e.OpensAt)
            .HasColumnName("opens_at")
            .IsRequired();

        builder.Property(e => e.ClosesAt)
            .HasColumnName("closes_at")
            .IsRequired();

        builder.Property(e => e.Cadence)
            .HasColumnName("cadence")
            .IsRequired();
    }
}
