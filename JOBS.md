# Jobs Functionality

This document describes the background jobs functionality implemented in Admitto.

## Overview

The jobs system supports two types of jobs:

1. **Regular Jobs** - Execute as soon as possible with concurrency control
2. **Scheduled Jobs** - Execute on a recurring schedule based on cron expressions

## Architecture

- **Job Interfaces**: `IJob`, `IJobHandler<T>`, `IJobProgress`, `IJobRunner`
- **Domain Entities**: `Job` (execution tracking), `ScheduledJob` (schedule management)
- **Infrastructure**: `JobRunner` (orchestration), `JobsWorker` (background processing)
- **Database Tables**: `jobs`, `scheduled_jobs`

## Usage

### Defining a Job

```csharp
public class SendEmailJob : IJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public string RecipientEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class SendEmailJobHandler : IJobHandler<SendEmailJob>
{
    public async ValueTask Handle(SendEmailJob job, IJobProgress jobProgress, CancellationToken cancellationToken = default)
    {
        await jobProgress.ReportProgressAsync("Starting email send", 0, cancellationToken);
        
        // Your job logic here
        
        await jobProgress.ReportProgressAsync("Email sent successfully", 100, cancellationToken);
    }
}
```

### Starting a Regular Job

```csharp
var jobRunner = serviceProvider.GetRequiredService<IJobRunner>();

var job = new SendEmailJob 
{ 
    RecipientEmail = "user@example.com",
    Subject = "Welcome!",
    Body = "Thank you for registering!"
};

await jobRunner.StartJob(job);
```

### Scheduling a Recurring Job

```csharp
var job = new PurgeExpiredRegistrationsJob 
{ 
    MaxExpireTime = TimeSpan.FromDays(30) 
};

// Schedule to run daily at midnight
await jobRunner.AddOrUpdateScheduledJob(job, "0 0 * * *");
```

## Features

### Unit of Work Support
Jobs are started within a database transaction, ensuring that database updates and job creation are atomic.

### Progress Reporting
Jobs can report progress and status updates through the `IJobProgress` interface.

### Orphaned Job Recovery
The `JobsWorker` monitors for jobs that appear to be orphaned (running too long) and re-queues them for execution.

### State Recovery
On application startup, the worker reloads the state of currently running jobs.

### Messaging Integration
Jobs use the existing messaging infrastructure for triggering execution, ensuring reliable delivery.

## Configuration

Configure the `JobsWorker` in `appsettings.json`:

```json
{
  "JobsWorker": {
    "ScheduledJobsCheckInterval": "00:01:00",
    "OrphanedJobsCheckInterval": "00:05:00", 
    "OrphanedJobThreshold": "00:30:00"
  }
}
```

## Database Schema

### jobs table
- `id` (uuid, primary key)
- `job_type` (varchar, the full type name)
- `job_data` (jsonb, serialized job data)
- `status` (varchar, Pending/Running/Completed/Failed)
- `created_at` (timestamptz)
- `started_at` (timestamptz, nullable)
- `completed_at` (timestamptz, nullable)
- `error_message` (varchar, nullable)
- `progress_message` (varchar, nullable)
- `progress_percent` (integer, nullable)

### scheduled_jobs table
- `id` (uuid, primary key)
- `job_type` (varchar, the full type name)
- `job_data` (jsonb, serialized job data)
- `cron_expression` (varchar, cron schedule)
- `next_run_time` (timestamptz)
- `last_run_time` (timestamptz, nullable)
- `is_enabled` (boolean)

## Testing

The system includes integration tests that demonstrate the core functionality:

```csharp
[TestMethod]
public async Task StartJob_ShouldCreateJobInDatabase()
{
    var jobRunner = scope.ServiceProvider.GetRequiredService<IJobRunner>();
    var job = new SendEmailJob { /* ... */ };
    
    await jobRunner.StartJob(job);
    
    // Verify job was created in database
}
```

## Dependencies

- **Cronos**: For parsing and evaluating cron expressions
- **Entity Framework Core**: For database persistence
- **Azure Storage Queues**: For messaging (via existing infrastructure)