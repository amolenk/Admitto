using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.UseCases.TicketedEvents.GetEventCreationRequest;

internal sealed record GetEventCreationRequestQuery(Guid TeamId, Guid CreationRequestId)
    : Query<EventCreationRequestDto>;
