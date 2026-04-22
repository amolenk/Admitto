using Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEvents.GetTicketedEventId;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases;

internal class RegistrationsTicketedEventIdLookup(IMediator mediator) : ITicketedEventIdLookup
{
    public async ValueTask<Guid> GetTicketedEventIdAsync(
        Guid teamId,
        string eventSlug,
        CancellationToken cancellationToken = default) =>
        await mediator.QueryAsync<GetTicketedEventIdQuery, Guid>(
            new GetTicketedEventIdQuery(teamId, eventSlug),
            cancellationToken);
}
