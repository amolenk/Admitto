using Microsoft.EntityFrameworkCore.Design;
using Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

namespace Amolenk.Admitto.Core.Email.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="EmailDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public sealed class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EmailDbContext>();
        optionsBuilder.UseModuleNpgsql<EmailDbContext>();

        return new EmailDbContext(optionsBuilder.Options);
    }
}
