using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetReconfirmDeliveryState;

internal sealed record GetReconfirmDeliveryStateQuery(
    TeamId TeamId,
    TicketedEventId EventId,
    ReconfirmDeliveryQuery DeliveryQuery)
    : Query<ReconfirmDeliveryState>;
