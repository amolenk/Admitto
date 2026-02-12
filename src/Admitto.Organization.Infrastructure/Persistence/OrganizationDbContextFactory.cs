using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amolenk.Admitto.Organization.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="OrganizationDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public sealed class OrganizationDbContextFactory : IDesignTimeDbContextFactory<OrganizationDbContext>
{
    public OrganizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrganizationDbContext>();
        optionsBuilder.UseNpgsql();
        
        return new OrganizationDbContext(optionsBuilder.Options);
    }
}