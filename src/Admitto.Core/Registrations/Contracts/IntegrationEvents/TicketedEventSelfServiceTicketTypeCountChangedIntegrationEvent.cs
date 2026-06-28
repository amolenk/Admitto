using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

[method: JsonConstructor]
public sealed record TicketedEventSelfServiceTicketTypeCountChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    uint TicketCatalogVersion,
    int SelfServiceTicketTypeCount) : IntegrationEvent
{
    public TicketedEventSelfServiceTicketTypeCountChangedIntegrationEvent(
        Guid TeamId,
        Guid TicketedEventId,
        int SelfServiceTicketTypeCount)
        : this(TeamId, TicketedEventId, TicketCatalogVersion: 0, SelfServiceTicketTypeCount)
    {
    }
}
