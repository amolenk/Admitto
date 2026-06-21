namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

internal static class DesignTimeDbContextOptions
{
    public static void UseModuleNpgsql<TDbContext>(this DbContextOptionsBuilder<TDbContext> optionsBuilder)
        where TDbContext : DbContext, IModuleDbContext
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__admitto-db")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings:admitto-db");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'admitto-db' is not set.");

            optionsBuilder.UseNpgsql(ModuleNpgsqlOptions.ConfigureMigrationsHistory<TDbContext>);
            return;
        }

        optionsBuilder.UseNpgsql(
            connectionString,
            ModuleNpgsqlOptions.ConfigureMigrationsHistory<TDbContext>);
    }
}
