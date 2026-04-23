using Amolenk.Admitto.Module.Email.Application;
using Amolenk.Admitto.Module.Organization.Application;
using Amolenk.Admitto.Module.Registrations.Application;
using Amolenk.Admitto.Module.Registrations.Infrastructure;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Infrastructure;
using Amolenk.Admitto.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// The Worker has no HTTP context, so provide a fixed system identity
// for the AuditInterceptor used by EF Core.
builder.Services.AddSingleton<IUserContextAccessor, SystemUserContextAccessor>();

// Add Organization module services (with Jobs capability to register Quartz jobs).
builder
    .AddOrganizationApplicationServices(HostCapability.Jobs)
    .AddOrganizationInfrastructureServices()
    .AddOrganizationIdentityServices();

// Add Registrations module services. The Worker hosts the queue consumer so it
// must be able to handle integration and module events targeted at Registrations.
builder.Services.AddRegistrationsApplicationServices(HostCapability.Jobs);
builder.AddRegistrationsInfrastructureServices();

// Add Email module services (to keep encrypted secrets decryptable here).
builder
    .AddEmailApplicationServices(HostCapability.Jobs | HostCapability.Email)
    .AddEmailInfrastructureServices(HostCapability.Email);

// Add shared services.
builder
    .AddSharedInfrastructureMessagingServices()
    .AddSharedInfrastructureQueueConsumer();

builder.Services
    .AddMessagingApplicationServices()
    .AddSharedInfrastructureServices();

var host = builder.Build();
host.Run();