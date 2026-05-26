namespace Amolenk.Admitto.Core.Registrations.Contracts;

/// <summary>
/// Per-registration projection returned by
/// <see cref="IRegistrationsFacade.QueryRegistrationsAsync"/>. Carries the
/// minimum surface other modules need to materialise recipient lists and
/// per-recipient template parameters without crossing the Registrations
/// aggregate boundary.
/// </summary>
public sealed record RegistrationListItemDto(
    Guid RegistrationId,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyCollection<Guid> TicketTypeIds,
    IReadOnlyDictionary<string, string> AdditionalDetails,
    DateTimeOffset CreatedAt,
    RegistrationStatus Status,
    bool HasReconfirmed,
    DateTimeOffset? ReconfirmedAt,
    int? EffectiveMaxReconfirmAttempts);
