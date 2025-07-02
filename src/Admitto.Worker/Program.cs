using Amolenk.Admitto.Infrastructure.Jobs;
using Amolenk.Admitto.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.Configure<MessageQueuesWorkerOptions>(builder.Configuration.GetSection(
    MessageQueuesWorkerOptions.SectionName));
builder.Services.AddOptions<MessageQueuesWorkerOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.Configure<JobsWorkerOptions>(builder.Configuration.GetSection(
    JobsWorkerOptions.SectionName));
builder.Services.AddOptions<JobsWorkerOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

// builder.Services.AddHostedService<MessageOutboxWorker>();
builder.Services.AddHostedService<MessageQueuesWorker>();
builder.Services.AddHostedService<JobsWorker>();

// TODO Move to ServiceDefaults
// builder.Services.AddDefaultApplicationServices();
builder.Services.AddCommandHandlers();
builder.Services.AddJobHandlers();
builder.Services.AddEventualDomainEventHandlers();
    
builder.AddDefaultInfrastructureServices();
builder.AddSmtpEmailServices();

var host = builder.Build();
host.Run();
