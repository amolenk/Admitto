using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink;

internal sealed class CreateScannerLinkHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider,
    IOptions<ScannerLinksOptions> options)
    : ICommandHandler<CreateScannerLinkCommand, ScannerLinkDto>
{
    public async ValueTask<ScannerLinkDto> HandleAsync(
        CreateScannerLinkCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await GetEventAsync(command, cancellationToken);
        var now = timeProvider.GetUtcNow();

        ticketedEvent.CreateScannerLink(ScannerLinkSecret.New(), now);
        return ScannerLinkDtoFactory.Create(ticketedEvent, now, options);
    }

    private async ValueTask<TicketedEvent> GetEventAsync(
        CreateScannerLinkCommand command,
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
