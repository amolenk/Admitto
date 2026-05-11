using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// The Worker has no HTTP context, so provide a fixed system identity
// for the AuditInterceptor used by EF Core.
builder.Services.AddSingleton<IUserContextAccessor, SystemUserContextAccessor>();

// Add modules (application + infrastructure) and their worker-specific services.
builder
    .AddOrganizationModule()
    .AddOrganizationModuleWorker()
    .AddOrganizationIdentityServices();

builder
    .AddRegistrationsModule()
    .AddRegistrationsModuleWorker();

builder
    .AddEmailModule()
    .AddEmailModuleWorker();

// Add shared services.
builder
    .AddSharedInfrastructureMessagingServices()
    .AddSharedInfrastructureQueueConsumer();

builder.Services
    .AddCryptographyApplicationServices()
    .AddSharedInfrastructureServices();

builder.AddMessageTypeRegistry(b =>
{
    b.AddOrganizationMessageTypes();
    b.AddRegistrationsMessageTypes();
    b.AddEmailMessageTypes();
});

var host = builder.Build();
host.Run();