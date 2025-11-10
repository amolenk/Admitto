using Amolenk.Admitto.Application.Jobs.SendCustomBulkEmail;
using Amolenk.Admitto.Worker;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.Configure<MessageQueuesWorkerOptions>(builder.Configuration.GetSection(
    MessageQueuesWorkerOptions.SectionName));
builder.Services.AddOptions<MessageQueuesWorkerOptions>()
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});

// builder.Services.AddHostedService<MessageOutboxWorker>();
builder.Services.AddHostedService<MessageQueuesWorker>();

// TODO Move to ServiceDefaults
// builder.Services.AddDefaultApplicationServices();
builder.Services.AddCommandHandlers();
builder.Services.AddEventualDomainEventHandlers();
builder.Services.AddApplicationEventHandlers();
builder.Services.AddEmailServices();

builder.AddDefaultInfrastructureServices();

var host = builder.Build();
host.Run();
