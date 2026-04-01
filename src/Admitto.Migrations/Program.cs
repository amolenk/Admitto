using Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Registrations.Infrastructure;
using Amolenk.Admitto.Module.Registrations.Infrastructure.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Migrations run without an HTTP context, so provide a no-op IUserContextAccessor
// to satisfy the AuditInterceptor dependency.
builder.Services.AddSingleton<IUserContextAccessor>(new MigrationUserContextAccessor());

builder.AddOrganizationInfrastructureServices();
builder.AddRegistrationsInfrastructureServices();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();

await MigrateDatabasesAsync<OrganizationDbContext>(migrationScope);
await MigrateDatabasesAsync<RegistrationsDbContext>(migrationScope);
return;

async ValueTask MigrateDatabasesAsync<TDbContext>(IServiceScope scope) where TDbContext : DbContext
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
    await dbContext.Database.MigrateAsync();
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
