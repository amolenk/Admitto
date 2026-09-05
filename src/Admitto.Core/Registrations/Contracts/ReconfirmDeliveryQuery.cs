namespace Amolenk.Admitto.Core.Registrations.Contracts;

/// <summary>
/// Authoritative delivery-time check for one queued reconfirmation recipient.
/// The ticket selection is the selection captured in the Email snapshot.
/// </summary>
public sealed record ReconfirmDeliveryQuery(
    Guid RegistrationId,
    Guid RegistrationCycleId,
    IReadOnlyCollection<Guid> ExpectedTicketTypeIds,
    DateTimeOffset Now);
