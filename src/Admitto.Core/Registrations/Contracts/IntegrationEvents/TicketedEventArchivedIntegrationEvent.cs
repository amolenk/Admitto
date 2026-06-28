using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Registrations module when a <c>TicketedEvent</c> transitions
/// to <c>Archived</c>. The Organization module consumes this to advance the
/// owning team's counters (active or cancelled → archived); it determines the
/// source counter from its locally-tracked event record.
/// </summary>
[method: JsonConstructor]
public sealed record TicketedEventArchivedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    uint TicketedEventVersion) : IntegrationEvent
{
    public TicketedEventArchivedIntegrationEvent(Guid TeamId, Guid TicketedEventId)
        : this(TeamId, TicketedEventId, TicketedEventVersion: 0)
    {
    }
}
