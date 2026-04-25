using Amolenk.Admitto.Module.Email.Application;
using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Persistence.ValueConverters;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence;

public sealed class EmailDbContext(DbContextOptions<EmailDbContext> options)
    : DbContext(options), IModuleDbContext, IEmailWriteStore, IOutboxDbContext, IDataProtectionKeyContext
{
    public static string SchemaName => "email";

    public DbSet<EmailSettings> EmailSettings => Set<EmailSettings>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<EmailLog> EmailLog => Set<EmailLog>();
    public DbSet<BulkEmailJob> BulkEmailJobs => Set<BulkEmailJob>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailDbContext).Assembly);

        // Data Protection key ring is shared across hosts via this table.
        modelBuilder.Entity<DataProtectionKey>(b =>
        {
            b.ToTable("data_protection_keys");
            b.Property(k => k.Id).HasColumnName("id");
            b.Property(k => k.FriendlyName).HasColumnName("friendly_name");
            b.Property(k => k.Xml).HasColumnName("xml");
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSharedConventions();

        configurationBuilder
            .Properties<Hostname>()
            .HaveConversion<HostnameConverter>();

        configurationBuilder
            .Properties<Port>()
            .HaveConversion<PortConverter>();

        configurationBuilder
            .Properties<SmtpUsername>()
            .HaveConversion<SmtpUsernameConverter>();

        configurationBuilder
            .Properties<ProtectedPassword>()
            .HaveConversion<ProtectedPasswordConverter>();
    }
}
