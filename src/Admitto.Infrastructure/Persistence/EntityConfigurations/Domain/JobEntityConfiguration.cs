using Amolenk.Admitto.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Amolenk.Admitto.Infrastructure.Persistence.EntityConfigurations.Domain;

public class JobEntityConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs");
        
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

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasConversion<string>();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.StartedAt)
            .HasColumnName("started_at");

        builder.Property(e => e.CompletedAt)
            .HasColumnName("completed_at");

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        builder.Property(e => e.ProgressMessage)
            .HasColumnName("progress_message")
            .HasMaxLength(500);

        builder.Property(e => e.ProgressPercent)
            .HasColumnName("progress_percent");

        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.JobType);
        builder.HasIndex(e => e.CreatedAt);
    }
}