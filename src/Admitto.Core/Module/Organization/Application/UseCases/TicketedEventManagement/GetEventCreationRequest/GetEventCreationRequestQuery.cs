using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.UseCases.TicketedEventManagement.GetEventCreationRequest;

internal sealed record GetEventCreationRequestQuery(Guid TeamId, Guid CreationRequestId)
    : Query<EventCreationRequestDto>;
