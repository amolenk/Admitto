using Amolenk.Admitto.Application.Common.Cryptography;
using Amolenk.Admitto.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.Configure<MessageQueuesWorkerOptions>(builder.Configuration.GetSection(
    MessageQueuesWorkerOptions.SectionName));
builder.Services.AddOptions<MessageQueuesWorkerOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

// builder.Services.Configure<JobsOptions>(builder.Configuration.GetSection(
//     JobsOptions.SectionName));
// builder.Services.AddOptions<JobsOptions>()
//     .ValidateDataAnnotations()
//     .ValidateOnStart();

// builder.Services.AddHostedService<MessageOutboxWorker>();
builder.Services.AddHostedService<MessageQueuesWorker>();

// builder.Services.AddSingleton<JobsWorker>();
// builder.Services.AddHostedService(provider => provider.GetRequiredService<JobsWorker>());
// builder.Services.AddSingleton<IJobsWorker>(sp => sp.GetRequiredService<JobsWorker>());

// builder.Services.AddScoped<IJobScheduler, JobScheduler>();


// TODO Move to ServiceDefaults
// builder.Services.AddDefaultApplicationServices();
builder.Services.AddCommandHandlers();
builder.Services.AddJobHandlers();
builder.Services.AddEventualDomainEventHandlers();
builder.Services.AddApplicationEventHandlers();
builder.Services.AddEmailServices();

builder.AddDefaultInfrastructureServices();

var host = builder.Build();
host.Run();
