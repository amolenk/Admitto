using System.Text.Json.Serialization;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Contracts.IntegrationEvents;

 [method: JsonConstructor]
public sealed record ReconfirmAutoExpiredRegistrationReference(
    Guid RegistrationId,
    Guid? RegistrationCycleId,
    uint? RegistrationVersion = null,
    uint? TicketCatalogVersion = null,
    IReadOnlyCollection<Guid>? TicketTypeIds = null);

[method: JsonConstructor]
public sealed record ReconfirmAutoExpiredIntegrationEvent(
    Guid TeamId,
    Guid TicketedEventId,
    IReadOnlyCollection<Guid> RegistrationIds,
    IReadOnlyCollection<ReconfirmAutoExpiredRegistrationReference>? RegistrationReferences = null) : IntegrationEvent;
