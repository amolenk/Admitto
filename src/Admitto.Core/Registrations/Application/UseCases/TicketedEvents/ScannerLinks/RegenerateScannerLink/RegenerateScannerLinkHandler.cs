using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink;

internal sealed class RegenerateScannerLinkHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider,
    IOptions<ScannerLinksOptions> options)
    : ICommandHandler<RegenerateScannerLinkCommand, ScannerLinkDto>
{
    public async ValueTask<ScannerLinkDto> HandleAsync(
        RegenerateScannerLinkCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await GetEventAsync(command, cancellationToken);
        var now = timeProvider.GetUtcNow();

        ticketedEvent.RegenerateScannerLink(ScannerLinkSecret.New(), now);
        return ScannerLinkDtoFactory.Create(ticketedEvent, now, options);
    }

    private async ValueTask<TicketedEvent> GetEventAsync(
        RegenerateScannerLinkCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.FirstOrDefaultAsync(
            e => e.Id == TicketedEventId.From(command.EventId)
                 && e.TeamId == TeamId.From(command.TeamId),
            cancellationToken);

        return ticketedEvent ?? throw new BusinessRuleViolationException(
            NotFoundError.Create<TicketedEvent>());
    }
}
