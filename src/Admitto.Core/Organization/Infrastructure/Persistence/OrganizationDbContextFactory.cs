using Microsoft.EntityFrameworkCore.Design;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

namespace Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="OrganizationDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();
        optionsBuilder.UseModuleNpgsql<OrganizationDbContext>();

        return new OrganizationDbContext(optionsBuilder.Options);
    }
}
