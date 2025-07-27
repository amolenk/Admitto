using System.Text.Json;
using Amolenk.Admitto.Infrastructure.Auth;
using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Migration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
    var openFgaOptions = configuration.GetSection("OpenFGA").Get<OpenFgaOptions>();
    
    var openFgaMigrator = host.Services.GetRequiredService<OpenFgaMigrator>();
    await openFgaMigrator.MigrateAsync(openFgaOptions?.AdminUserIds ?? []);
}