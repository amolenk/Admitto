using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.AttendeeEmails.GetAttendeeEmails;

internal sealed class GetAttendeeEmailsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetAttendeeEmailsQuery, IReadOnlyList<AttendeeEmailLogItemDto>>
{
    public async ValueTask<IReadOnlyList<AttendeeEmailLogItemDto>> HandleAsync(
        GetAttendeeEmailsQuery query,
        CancellationToken cancellationToken)
    {
        var entries = await writeStore.EmailLog
            .Where(e => e.TicketedEventId == query.EventId
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
