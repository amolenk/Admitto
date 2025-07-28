using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations;

public class ScheduledJobEntityConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.ToTable("scheduled_jobs");
        
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.JobType)
            .HasColumnName("job_type")
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.JobData)
            .HasColumnName("job_data")
            .IsRequired()
            .HasColumnType("jsonb");

        builder.Property(e => e.CronExpression)
            .HasColumnName("cron_expression")
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.NextRunTime)
            .HasColumnName("next_run_time")
            .IsRequired();

        builder.Property(e => e.LastRunTime)
            .HasColumnName("last_run_time");

        builder.Property(e => e.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired();

        builder.HasIndex(e => e.NextRunTime);
        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.JobType);
    }
}