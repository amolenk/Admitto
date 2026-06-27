using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence;

public sealed class EmailDbContext(DbContextOptions<EmailDbContext> options)
    : DbContext(options), IModuleDbContext, IEmailWriteStore, IOutboxDbContext
{
    public static string SchemaName => "email";

    public DbSet<EmailLog> EmailLog => Set<EmailLog>();
    public DbSet<BulkEmailJob> BulkEmailJobs => Set<BulkEmailJob>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EmailDbContext).Assembly,
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Email") == true);
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
            .Properties<BulkEmailJobId>()
            .HaveConversion<BulkEmailJobId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailLogId>()
            .HaveConversion<EmailLogId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailAccentColor>()
            .HaveConversion<EmailAccentColor.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailFontFamily>()
            .HaveConversion<EmailFontFamily.EfCoreValueConverter>();
    }
}
