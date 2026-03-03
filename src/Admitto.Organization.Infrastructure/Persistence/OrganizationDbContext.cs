using Amolenk.Admitto.Organization.Application.Persistence;
using Amolenk.Admitto.Organization.Domain.Entities;
using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Organization.Infrastructure.Persistence.ValueConverters;
using Amolenk.Admitto.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence;

public sealed class OrganizationDbContext(DbContextOptions<OrganizationDbContext> options)
    : DbContext(options), IModuleDbContext, IOrganizationWriteStore
{
    public static string SchemaName => "organization";

    public DbSet<Team> Teams => Set<Team>();

    public DbSet<TicketedEvent> TicketedEvents => Set<TicketedEvent>();
    
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("organization");
        modelBuilder.ApplySharedConfiguration();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OrganizationDbContext).Assembly);
    }
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.ConfigureSharedConventions();
        
        configurationBuilder
            .Properties<UserId>()
            .HaveConversion<UserIdConverter>();

        configurationBuilder
            .Properties<ExternalUserId>()
            .HaveConversion<ExternalUserIdConverter>();
    }
}
