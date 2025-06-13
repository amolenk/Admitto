using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using static System.Guid;

var builder = Host.CreateApplicationBuilder();

builder.AddServiceDefaults();
builder.AddDefaultInfrastructureServices();

if (!builder.Environment.IsDevelopment())
{
    Console.WriteLine("Migration is only supported in development mode.");
    return 1;
}

var host = builder.Build();

await Task.WhenAll(
    MigrateDatabaseAsync(),
    MigrateOpenFgaAsync(builder.Configuration));

return 0;

async Task MigrateDatabaseAsync()
{
    using var scope = host.Services.CreateScope();
    
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    
    await dbContext.Database.MigrateAsync();
}

async Task MigrateOpenFgaAsync(IConfiguration configuration)
{
#pragma warning disable CA1806
    TryParse(configuration["OpenFga:AdminUserId"], out var adminUserId);
#pragma warning restore CA1806
    
    var openFgaMigrator = host.Services.GetRequiredService<OpenFgaMigrator>();
    await openFgaMigrator.MigrateAsync(adminUserId);
}