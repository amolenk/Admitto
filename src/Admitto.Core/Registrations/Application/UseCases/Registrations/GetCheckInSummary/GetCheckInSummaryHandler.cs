using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetCheckInSummary;

internal sealed class GetCheckInSummaryHandler(IRegistrationsWriteStore writeStore)
    : IQueryHandler<GetCheckInSummaryQuery, CheckInSummaryDto?>
{
    public async ValueTask<CheckInSummaryDto?> HandleAsync(
        GetCheckInSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var eventExists = await writeStore.TicketedEvents
            .AsNoTracking()
            .AnyAsync(
                e => e.Id == TicketedEventId.From(query.EventId) && e.TeamId == TeamId.From(query.TeamId),
                cancellationToken);

        if (!eventExists)
            return null;

        var registrations = writeStore.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == TicketedEventId.From(query.EventId) && r.TeamId == TeamId.From(query.TeamId));

        var expectedCount = await registrations
            .CountAsync(r => r.Status == RegistrationStatus.Registered, cancellationToken);
        var checkedInCount = await registrations
            .CountAsync(r => r.Status == RegistrationStatus.Registered && r.CheckedInAt != null, cancellationToken);

        return new CheckInSummaryDto(checkedInCount, expectedCount);
    }
}
