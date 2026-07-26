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
    string TimeZone) : IntegrationEvent;
