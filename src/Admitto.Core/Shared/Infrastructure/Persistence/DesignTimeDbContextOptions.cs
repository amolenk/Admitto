namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

internal static class DesignTimeDbContextOptions
{
    public static void UseModuleNpgsql<TDbContext>(this DbContextOptionsBuilder<TDbContext> optionsBuilder)
        where TDbContext : DbContext, IModuleDbContext
    {
        // Use the actual connection string if available for EF migrations.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__admitto-db");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            optionsBuilder.UseNpgsql(
                connectionString,
                ModuleNpgsqlOptions.ConfigureMigrationsHistory<TDbContext>);
        }
        else
        {
            optionsBuilder.UseNpgsql(ModuleNpgsqlOptions.ConfigureMigrationsHistory<TDbContext>);
        }
    }
}
