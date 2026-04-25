using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmail;

internal sealed class GetBulkEmailHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetBulkEmailQuery, BulkEmailJobDetailDto?>
{
    public async ValueTask<BulkEmailJobDetailDto?> HandleAsync(GetBulkEmailQuery query, CancellationToken ct)
    {
        var job = await writeStore.BulkEmailJobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == query.BulkEmailJobId, ct);

        if (job is null)
            return null;

        var recipients = job.Recipients
            .Select(r => new BulkEmailRecipientDto(
                r.Email,
                r.DisplayName,
                r.RegistrationId,
                r.Status,
                r.LastError))
            .ToList();

        return new BulkEmailJobDetailDto(
            job.Id.Value,
            job.TeamId.Value,
            job.TicketedEventId.Value,
            job.EmailType,
            job.Subject,
            job.TextBody,
            job.HtmlBody,
            job.Source,
            job.Status,
            job.RecipientCount,
            job.SentCount,
            job.FailedCount,
            job.CancelledCount,
            job.LastError,
            job.IsSystemTriggered,
            job.TriggeredBy?.Value,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.CancellationRequestedAt,
            job.CancelledAt,
            job.Version,
            recipients);
    }
}
