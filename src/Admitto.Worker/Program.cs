using Amolenk.Admitto.Core.Email.Application;
using Amolenk.Admitto.Core.Organization.Application;
using Amolenk.Admitto.Core.Registrations.Application;
using Amolenk.Admitto.Core.Registrations.Infrastructure;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Infrastructure;
using Amolenk.Admitto.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// The Worker has no HTTP context, so provide a fixed system identity
// for the AuditInterceptor used by EF Core.
builder.Services.AddSingleton<IUserContextAccessor, SystemUserContextAccessor>();

// Add Organization module services.
builder
    .AddOrganizationModule()
    .AddOrganizationModuleWorker()
    .AddOrganizationInfrastructureServices()
    .AddOrganizationIdentityServices();

// Add Registrations module services.
builder
    .AddRegistrationsModule()
    .AddRegistrationsModuleWorker()
    .AddRegistrationsInfrastructureServices();

// Add Email module services.
builder
    .AddEmailModule()
    .AddEmailModuleWorker()
    .AddEmailInfrastructureServices();

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