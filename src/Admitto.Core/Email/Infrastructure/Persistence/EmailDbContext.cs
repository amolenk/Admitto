using Amolenk.Admitto.Core.Email.Application;
using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Persistence.ValueConverters;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence;

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
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailDbContext).Assembly,
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Email") == true);

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
        configurationBuilder
            .Properties<TeamId>()
            .HaveConversion<TeamId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketedEventId>()
            .HaveConversion<TicketedEventId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailScopeId>()
            .HaveConversion<EmailScopeId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<RegistrationId>()
            .HaveConversion<RegistrationId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailAddress>()
            .HaveConversion<EmailAddress.EfCoreValueConverter>();

        configurationBuilder
            .Properties<Hostname>()
            .HaveConversion<Hostname.EfCoreValueConverter>();

        configurationBuilder
            .Properties<Port>()
            .HaveConversion<Port.EfCoreValueConverter>();

        configurationBuilder
            .Properties<SmtpUsername>()
            .HaveConversion<SmtpUsername.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ProtectedPassword>()
            .HaveConversion<ProtectedPasswordConverter>();

        configurationBuilder
            .Properties<BulkEmailJobId>()
            .HaveConversion<BulkEmailJobId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailLogId>()
            .HaveConversion<EmailLogId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailSettingsId>()
            .HaveConversion<EmailSettingsId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailTemplateId>()
            .HaveConversion<EmailTemplateId.EfCoreValueConverter>();
    }
}
