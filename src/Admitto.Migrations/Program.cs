using Amolenk.Admitto.Application.Common.Abstractions;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddDefaultInfrastructureServices();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();
var migrationService = migrationScope.ServiceProvider.GetRequiredService<IMigrationService>();
await migrationService.MigrateAllAsync();
