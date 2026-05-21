using Amolenk.Admitto.Core.Shared.Application.Auth;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// The Worker has no HTTP context, so provide a fixed system identity
// for the AuditInterceptor used by EF Core.
builder.Services.AddSingleton<IUserContextAccessor>(
    new StaticUserContextAccessor(StaticUserContextAccessor.SystemUser));

// Add modules (application + infrastructure) and their worker-specific services.
builder.AddBadgesModuleWorker();
builder.AddEmailModuleWorker();
builder.AddOrganizationModuleWorker();
builder.AddOrganizationIdentityServices();
builder.AddRegistrationsModuleWorker();

// Add shared services.
builder
    .AddSharedServices()
    .AddSharedInfrastructureQueueConsumer();

var host = builder.Build();
host.Run();
