using Microsoft.EntityFrameworkCore.Design;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

namespace Amolenk.Admitto.Core.Badges.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="BadgesDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public class BadgesDbContextFactory : IDesignTimeDbContextFactory<BadgesDbContext>
{
    public BadgesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BadgesDbContext>();
        optionsBuilder.UseModuleNpgsql<BadgesDbContext>();

        return new BadgesDbContext(optionsBuilder.Options);
    }
}
