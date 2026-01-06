using Amolenk.Admitto.Application.Common.Migration;
using Amolenk.Admitto.Infrastructure.Migrators;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddTransient<BetterAuthMigrator>();
builder.Services.AddTransient<QuartzMigrator>();
builder.Services.AddTransient<IMigrationService, MigrationService>();

var app = builder.Build();

using var migrationScope = app.Services.CreateScope();
var migrationService = migrationScope.ServiceProvider.GetRequiredService<IMigrationService>();
await migrationService.MigrateAllAsync();

