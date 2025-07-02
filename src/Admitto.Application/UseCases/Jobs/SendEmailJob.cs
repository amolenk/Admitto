using Amolenk.Admitto.Application.Common.Abstractions;

namespace Amolenk.Admitto.Application.UseCases.Jobs;

public class SendEmailJob : IJob
{
    public Guid Id { get; } = Guid.NewGuid();
    public string RecipientEmail { get; set; } = null!;
    public string Subject { get; set; } = null!;
    public string Body { get; set; } = null!;
}

public class SendEmailJobHandler(ILogger<SendEmailJobHandler> logger) : IJobHandler<SendEmailJob>
{
    public async ValueTask Handle(SendEmailJob job, IJobProgress jobProgress, CancellationToken cancellationToken = default)
    {
        await jobProgress.ReportProgressAsync("Starting email send", 0, cancellationToken);
        
        logger.LogInformation("Sending email to {Email} with subject {Subject}", 
            job.RecipientEmail, job.Subject);
        
        await jobProgress.ReportProgressAsync("Composing email", 25, cancellationToken);
        
        // Simulate email sending work
        await Task.Delay(1000, cancellationToken);
        
        await jobProgress.ReportProgressAsync("Sending email", 75, cancellationToken);
        
        // Simulate more work
        await Task.Delay(500, cancellationToken);
        
        await jobProgress.ReportProgressAsync("Email sent successfully", 100, cancellationToken);
        
        logger.LogInformation("Email sent successfully to {Email}", job.RecipientEmail);
    }
}