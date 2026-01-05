using Amolenk.Admitto.Worker;
using Quartz;

var builder = Host.CreateApplicationBuilder(args);

// Add default services.
builder.AddServiceDefaults();

// Add application services.
builder.Services
    .AddApplicationWorkerCommandHandlers()
    .AddApplicationApplicationEventHandlers()
    .AddApplicationEventualDomainEventHandlers()
    .AddApplicationTransactionalDomainEventHandlers()
    .AddApplicationJobs();

// Add application event handlers for events that are processed in the worker.
builder.Services
    .AddApplicationApplicationEventHandlers()
    .AddApplicationEventualDomainEventHandlers();

// Add Quartz.NET hosted service.
builder.Services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });

// Add email services.
builder.Services
    .AddApplicationEmailServices()
    .AddInfrastructureEmailServices();

// Add message queue processor for processing internal messages.
builder.Services
    .AddHostedService<MessageQueueProcessor>()
    .AddOptions<MessageQueueProcessorOptions>()
    .Bind(builder.Configuration.GetSection(MessageQueueProcessorOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var host = builder.Build();
host.Run();