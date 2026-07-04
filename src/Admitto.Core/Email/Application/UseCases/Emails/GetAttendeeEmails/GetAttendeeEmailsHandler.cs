using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.GetAttendeeEmails;

internal sealed class GetAttendeeEmailsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>>
{
    public async ValueTask<IReadOnlyList<AttendeeEmailLogItemDto>> HandleAsync(
        GetAttendeeEmailsQuery query,
        CancellationToken cancellationToken)
    {
        var entries = await writeStore.EmailLog
            .Where(e => e.TicketedEventId == query.EventId
                     && e.TeamId == query.TeamId
                     && e.RegistrationId == query.RegistrationId)
            .OrderByDescending(e => e.StatusUpdatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries
            .Select(e => new AttendeeEmailLogItemDto(
                Id: e.Id.Value,
                Subject: e.Subject,
                EmailType: e.EmailType,
                Status: e.Status.ToString(),
                SentAt: e.SentAt,
                BulkEmailJobId: e.BulkEmailJobId?.Value))
            .ToList();
    }
}
