using System.Reflection;
using Amolenk.Admitto.Core.Badges.Application.Persistence;
using Amolenk.Admitto.Core.Badges.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;

public sealed class BadgesDbContext(DbContextOptions<BadgesDbContext> options)
    : DbContext(options), IModuleDbContext, IBadgesWriteStore
{
    public static string SchemaName => "badges";

    public DbSet<BadgeEvent> BadgeEvents => Set<BadgeEvent>();
    public DbSet<BadgeType> BadgeTypes => Set<BadgeType>();
    public DbSet<BadgeInstance> BadgeInstances => Set<BadgeInstance>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly(),
            t => t.Namespace?.StartsWith(BadgesModule.NamespacePrefix) == true);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder
            .Properties<BadgeTypeId>()
            .HaveConversion<BadgeTypeId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<BadgeInstanceId>()
            .HaveConversion<BadgeInstanceId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<BadgeTypeName>()
            .HaveConversion<BadgeTypeName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<BadgeInstanceDisplayName>()
            .HaveConversion<BadgeInstanceDisplayName.EfCoreValueConverter>();

        configurationBuilder
            .Properties<BadgeInstanceNotes>()
            .HaveConversion<BadgeInstanceNotes.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TicketedEventId>()
            .HaveConversion<TicketedEventId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<TeamId>()
            .HaveConversion<TeamId.EfCoreValueConverter>();

        configurationBuilder
            .Properties<EmailAddress>()
            .HaveConversion<EmailAddress.EfCoreValueConverter>();
    }
}
