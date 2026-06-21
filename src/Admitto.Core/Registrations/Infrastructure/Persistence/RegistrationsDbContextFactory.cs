using Microsoft.EntityFrameworkCore.Design;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="RegistrationsDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public class RegistrationsDbContextFactory : IDesignTimeDbContextFactory<RegistrationsDbContext>
{
    public RegistrationsDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RegistrationsDbContext>();
        optionsBuilder.UseModuleNpgsql<RegistrationsDbContext>();
        
        return new RegistrationsDbContext(optionsBuilder.Options);
    }
}
