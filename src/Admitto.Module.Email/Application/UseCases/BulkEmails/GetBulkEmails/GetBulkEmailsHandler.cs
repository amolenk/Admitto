using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.BulkEmails.GetBulkEmails;

internal sealed class GetBulkEmailsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetBulkEmailsQuery, IReadOnlyList<BulkEmailListItemDto>>
{
    public async ValueTask<IReadOnlyList<BulkEmailListItemDto>> HandleAsync(
        GetBulkEmailsQuery query,
        CancellationToken ct)
    {
        var jobs = await writeStore.BulkEmailJobs
            .AsNoTracking()
            .Where(j => j.TicketedEventId == query.TicketedEventId)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);

        return jobs
            .Select(j => new BulkEmailListItemDto(
                j.Id.Value,
                j.EmailType,
                j.Status,
                j.RecipientCount,
                j.SentCount,
                j.FailedCount,
                j.CancelledCount,
                j.IsSystemTriggered,
                j.TriggeredBy?.Value,
                j.CreatedAt,
                j.StartedAt,
                j.CompletedAt,
                j.CancellationRequestedAt,
                j.CancelledAt))
            .ToList();
    }
}
