using Amolenk.Admitto.Infrastructure.Persistence;
using Amolenk.Admitto.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddHostedService<MessageOutboxWorker>();
builder.Services.AddHostedService<MessageQueuesWorker>();

// var databaseMigrationCompleted = false;
// builder.Services.AddHealthChecks()
//     .AddCheck("Database Migration", () => databaseMigrationCompleted
//         ? HealthCheckResult.Healthy("Database is up-to-date.")
//         : HealthCheckResult.Unhealthy("Database migrations are pending."));

// TODO Move to ServiceDefaults
builder.Services.AddApplicationServices();
builder.AddInfrastructureServices();

var host = builder.Build();

if (builder.Environment.IsDevelopment())
{
    // Migrate the database
    using var scope = host.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
    dbContext.Database.Migrate();
}

// databaseMigrationCompleted = true;

host.Run();
