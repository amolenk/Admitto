using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Amolenk.Admitto.Module.Email.Infrastructure.Persistence;

/// <summary>
/// Factory for creating <see cref="EmailDbContext"/> instances at design time.
/// Required for EF Core tools like migrations.
/// </summary>
public sealed class EmailDbContextFactory : IDesignTimeDbContextFactory<EmailDbContext>
{
    public EmailDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EmailDbContext>();
        optionsBuilder.UseNpgsql();

        return new EmailDbContext(optionsBuilder.Options);
    }
}
