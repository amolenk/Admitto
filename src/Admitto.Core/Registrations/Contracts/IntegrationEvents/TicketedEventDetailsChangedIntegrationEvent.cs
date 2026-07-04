using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;

[method: JsonConstructor]
public sealed record TicketedEventDetailsChangedIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    uint TicketedEventVersion,
    string Name,
    string WebsiteUrl,
    string PublicSlug,
    string TimeZone) : IntegrationEvent
{
    public TicketedEventDetailsChangedIntegrationEvent(
        Guid TeamId,
        Guid TicketedEventId,
        string Name,
        string WebsiteUrl,
        string PublicSlug,
        string TimeZone)
        : this(TeamId, TicketedEventId, TicketedEventVersion: 0, Name, WebsiteUrl, PublicSlug, TimeZone)
    {
    }
}
