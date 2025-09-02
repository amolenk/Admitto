using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.Jobs;
using Amolenk.Admitto.Application.Jobs.SendBulkEmail;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

builder.Services.AddEmailServices();
builder.Services.AddJobHandlers();
builder.AddDefaultInfrastructureServices();

var app = builder.Build();

app.MapPost("/jobs/{jobType}/run", (string jobType, IServiceProvider services) =>
{
    _ = Task.Run(() => RunJobAsync(jobType, services, CancellationToken.None));
    
    return Results.Ok("Job started");
});

app.Run();

return;

static async ValueTask RunJobAsync(string jobType, IServiceProvider serviceProvider, CancellationToken cancellationToken)
{
    using var scope = serviceProvider.CreateScope();
    
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<SendBulkEmailJobHandler>>();
    logger.LogInformation("Starting job '{JobType}'", jobType);

    if (!WellKnownJob.All.Contains(jobType))
    {
        logger.LogError("Unknown job type '{JobType}'", jobType);
        return;
    }
    
    try
    {
        var handler = scope.ServiceProvider.GetRequiredKeyedService<IJobHandler>(jobType);
        await handler.RunAsync(cancellationToken);

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
    }
    catch (Exception e)
    {
        logger.LogError(e, "Job failed");
    }
}