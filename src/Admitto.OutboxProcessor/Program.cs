using Admitto.OutboxProcessor;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddHostedService<Worker>();

// TODO Move to ServiceDefaults
builder.Services.AddApplicationServices();
builder.AddInfrastructureServices();

var host = builder.Build();
host.Run();