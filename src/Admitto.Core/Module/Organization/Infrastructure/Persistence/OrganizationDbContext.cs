using Amolenk.Admitto.Core.Module.Organization.Application.Persistence;
using Amolenk.Admitto.Core.Module.Organization.Domain.Entities;
using Amolenk.Admitto.Core.Module.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : DbContext(options), IModuleDbContext, IOrganizationWriteStore, IOutboxDbContext
{
    public static string SchemaName => "organization";

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<User> Users => Set<User>();

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfiguration(new OutboxMessageEntityConfiguration());
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly,
            t => t.Namespace?.StartsWith("Amolenk.Admitto.Core.Module.Organization") == true);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSharedConventions();

        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ApiKeyId>()
            .HaveConversion<ApiKeyId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<ExternalUserId>()
            .HaveConversion<ExternalUserId.EfCoreValueConverter>();
    }
}
