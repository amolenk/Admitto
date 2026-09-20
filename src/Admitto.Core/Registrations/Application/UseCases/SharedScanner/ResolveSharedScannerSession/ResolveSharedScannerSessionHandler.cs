using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.SharedScanner.ResolveSharedScannerSession;

internal sealed class ResolveSharedScannerSessionHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : IQueryHandler<ResolveSharedScannerSessionQuery, SharedScannerSessionDto>
{
    public async ValueTask<SharedScannerSessionDto> HandleAsync(
        ResolveSharedScannerSessionQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await SharedScannerAccess.ResolveActiveEventAsync(
            writeStore, query.Secret, timeProvider.GetUtcNow(), cancellationToken);

        return new SharedScannerSessionDto(
            ticketedEvent.TeamId.Value,
            ticketedEvent.Id.Value,
            ticketedEvent.Name.Value,
            ticketedEvent.StartsAt,
            ticketedEvent.TimeZone.Value);
    }
}
