using Amolenk.Admitto.Module.Organization.Application;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add Organization module services (with Jobs capability to register Quartz jobs).
builder
    .AddOrganizationApplicationServices(HostCapability.Jobs)
    .AddOrganizationInfrastructureServices()
    .AddOrganizationIdentityServices();

// Add shared services.
builder
    .AddSharedInfrastructureMessagingServices();

builder.Services
    .AddMessagingApplicationServices()
    .AddSharedInfrastructureServices();

var host = builder.Build();
host.Run();