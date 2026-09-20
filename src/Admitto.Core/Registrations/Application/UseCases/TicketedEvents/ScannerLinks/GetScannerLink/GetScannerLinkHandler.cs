using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.GetScannerLink;

internal sealed class GetScannerLinkHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider,
    IOptions<ScannerLinksOptions> options)
    : IQueryHandler<GetScannerLinkQuery, ScannerLinkDto?>
{
    public async ValueTask<ScannerLinkDto?> HandleAsync(
        GetScannerLinkQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.Id == TicketedEventId.From(query.EventId)
                     && e.TeamId == TeamId.From(query.TeamId),
                cancellationToken);

        return ticketedEvent is null
            ? null
            : ScannerLinkDtoFactory.Create(ticketedEvent, timeProvider.GetUtcNow(), options);
    }
}
