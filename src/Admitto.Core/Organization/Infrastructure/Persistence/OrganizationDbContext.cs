using Amolenk.Admitto.Core.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Inbox;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : DbContext(options), IModuleDbContext, IOrganizationWriteStore, IOutboxDbContext, IInboxDbContext
{
    public static string SchemaName => "organization";

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly,
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Organization") == true);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<TeamName>()
            .HaveConversion<TeamName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TeamAccentColor>()
            .HaveConversion<TeamAccentColor.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ApiKeyName>()
            .HaveConversion<ApiKeyName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailAddress>()
            .HaveConversion<EmailAddress.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TeamId>()
            .HaveConversion<TeamId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ApiKeyId>()
            .HaveConversion<ApiKeyId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ExternalUserId>()
            .HaveConversion<ExternalUserId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketedEventId>()
            .HaveConversion<TicketedEventId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<CreationRequestId>()
            .HaveConversion<CreationRequestId.EfCoreValueConverter>();
    }
}
