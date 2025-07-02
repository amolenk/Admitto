using Amolenk.Admitto.Application.Common.Abstractions;
using Amolenk.Admitto.Application.UseCases.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Amolenk.Admitto.Application.Examples;

// This example demonstrates how to use the jobs functionality
public class JobsExample
{
    public static async Task RunExample()
    {
        // Create a simple host to demonstrate job usage
        var builder = Host.CreateApplicationBuilder();
        
        // Add infrastructure services (this would normally be done via AddDefaultInfrastructureServices)
        // For this example, we're just showing the API usage pattern
        
        builder.Services.AddJobHandlers();
        builder.Services.AddScoped<IJobRunner, MockJobRunner>();
        
        var host = builder.Build();
        
        using var scope = host.Services.CreateScope();
        var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<JobsExample>>();
        
        logger.LogInformation("=== Jobs Example ===");
        
        // Example 1: Start a regular job
        logger.LogInformation("Starting regular job...");
        var emailJob = new SendEmailJob
        {
            RecipientEmail = "user@example.com",
            Subject = "Welcome to Admitto",
            Body = "Thank you for registering!"
        };
        
        await jobRunner.StartJob(emailJob);
        logger.LogInformation("Email job started with ID: {JobId}", emailJob.Id);
        
        // Example 2: Schedule a recurring job
        logger.LogInformation("Scheduling recurring job...");
        var purgeJob = new PurgeExpiredRegistrationsJob
        {
            MaxExpireTime = TimeSpan.FromDays(30)
        };
        
        await jobRunner.AddOrUpdateScheduledJob(purgeJob, "0 0 * * *"); // Daily at midnight
        logger.LogInformation("Purge job scheduled with ID: {JobId}", purgeJob.Id);
        
        logger.LogInformation("=== Jobs Example Complete ===");
    }
}

// Mock implementation for demonstration
public class MockJobRunner : IJobRunner
{
    private readonly ILogger<MockJobRunner> _logger;
    
    public MockJobRunner(ILogger<MockJobRunner> logger)
    {
        _logger = logger;
    }
    
    public ValueTask StartJob(IJob job, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Would start job {JobId} of type {JobType}", 
            job.Id, job.GetType().Name);
        return ValueTask.CompletedTask;
    }
    
    public ValueTask AddOrUpdateScheduledJob(IJob job, string cronExpression, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Mock: Would schedule job {JobId} with cron '{Cron}'", 
            job.Id, cronExpression);
        return ValueTask.CompletedTask;
    }
}