using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Infrastructure.Migrators;

public class DatabaseMigrator(ApplicationContext dbContext) : IMigrator
{
    public async ValueTask RunAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.MigrateAsync(cancellationToken: cancellationToken);
    }
}