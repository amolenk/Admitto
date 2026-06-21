using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace Amolenk.Admitto.Core.Shared.Infrastructure.Persistence;

public static class ModuleNpgsqlOptions
{
    public static void ConfigureMigrationsHistory<TDbContext>(NpgsqlDbContextOptionsBuilder npgsql)
        where TDbContext : DbContext, IModuleDbContext
    {
        npgsql.MigrationsHistoryTable("ef_migrations_history", TDbContext.SchemaName);
    }
}
