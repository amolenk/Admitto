using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest;

internal sealed record GetEventCreationRequestQuery(Guid TeamId, Guid CreationRequestId)
    : Query<EventCreationRequestDto>;
