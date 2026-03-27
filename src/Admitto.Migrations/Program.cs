using Amolenk.Admitto.Module.Organization.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrganizationInfrastructureServices();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();

await MigrateDatabasesAsync<OrganizationDbContext>(migrationScope);
// await MigrateDatabasesAsync<RegistrationsDbContext>(migrationScope);
return;

async ValueTask MigrateDatabasesAsync<TDbContext>(IServiceScope scope) where TDbContext : DbContext
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TDbContext>();
    await dbContext.Database.MigrateAsync();
}
