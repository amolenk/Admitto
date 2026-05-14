using Amolenk.Admitto.Core.Email.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Contracts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Migrations run without an HTTP context, so provide a no-op IUserContextAccessor
// to satisfy the AuditInterceptor dependency.
builder.Services.AddSingleton<IUserContextAccessor>(new MigrationUserContextAccessor());

builder.AddOrganizationModule();
builder.AddEmailModule();
builder.AddRegistrationsModule();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();

await MigrateDatabasesAsync<OrganizationDbContext>(migrationScope);
await MigrateDatabasesAsync<EmailDbContext>(migrationScope);
await MigrateDatabasesAsync<RegistrationsDbContext>(migrationScope);
await MigrateBetterAuthAsync(builder.Configuration);
return;

// BetterAuth schema — these tables are managed by the better-auth library (Node.js).
// We create them here using IF NOT EXISTS so the .NET migrations project can bootstrap
// a fresh database without requiring the Node.js CLI to be run separately.
const string BetterAuthSchemaSql = """
    CREATE TABLE IF NOT EXISTS "user" (
        id              TEXT PRIMARY KEY,
        name            TEXT NOT NULL,
        email           TEXT NOT NULL UNIQUE,
        "emailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
        image           TEXT,
        "createdAt"     TIMESTAMP NOT NULL DEFAULT NOW(),
        "updatedAt"     TIMESTAMP NOT NULL DEFAULT NOW()
    );

    CREATE TABLE IF NOT EXISTS session (
        id           TEXT PRIMARY KEY,
        "expiresAt"  TIMESTAMP NOT NULL,
        token        TEXT NOT NULL UNIQUE,
        "createdAt"  TIMESTAMP NOT NULL DEFAULT NOW(),
        "updatedAt"  TIMESTAMP NOT NULL DEFAULT NOW(),
        "ipAddress"  TEXT,
        "userAgent"  TEXT,
        "userId"     TEXT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS account (
        id                      TEXT PRIMARY KEY,
        "accountId"             TEXT NOT NULL,
        "providerId"            TEXT NOT NULL,
        "userId"                TEXT NOT NULL REFERENCES "user"(id) ON DELETE CASCADE,
        "accessToken"           TEXT,
        "refreshToken"          TEXT,
        "idToken"               TEXT,
        "accessTokenExpiresAt"  TIMESTAMP,
        "refreshTokenExpiresAt" TIMESTAMP,
        scope                   TEXT,
        password                TEXT,
        "createdAt"             TIMESTAMP NOT NULL DEFAULT NOW(),
        "updatedAt"             TIMESTAMP NOT NULL DEFAULT NOW()
    );

    CREATE TABLE IF NOT EXISTS verification (
        id          TEXT PRIMARY KEY,
        identifier  TEXT NOT NULL,
        value       TEXT NOT NULL,
        "expiresAt" TIMESTAMP NOT NULL,
        "createdAt" TIMESTAMP DEFAULT NOW(),
        "updatedAt" TIMESTAMP DEFAULT NOW()
    );
    """;

async ValueTask MigrateDatabasesAsync<TDbContext>(IServiceScope scope) where TDbContext : DbContext
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
    await dbContext.Database.MigrateAsync();
}

async ValueTask MigrateBetterAuthAsync(IConfiguration configuration)
{
    var connectionString = configuration.GetConnectionString("better-auth-db");
    if (string.IsNullOrWhiteSpace(connectionString))
        return;

    await using var dataSource = NpgsqlDataSource.Create(connectionString);
    await using var cmd = dataSource.CreateCommand(BetterAuthSchemaSql);
    await cmd.ExecuteNonQueryAsync();
}

/// <summary>
/// Stub accessor for migration-only scenarios where no user context exists.
/// The AuditInterceptor will not fire during migrations (no SaveChanges calls),
/// but its constructor still requires the service to be registered.
/// </summary>
file sealed class MigrationUserContextAccessor : IUserContextAccessor
{
    public UserContextDto Current => new(Guid.Empty, "migrations", "migrations@system.local");
}
