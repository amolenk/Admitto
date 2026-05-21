namespace Amolenk.Admitto.Core.Registrations.Contracts;

/// <summary>
/// Registration data projected for badge export use.
/// Returned by <see cref="IRegistrationsFacade.QueryRegistrationsForBadgeExportAsync"/>.
/// </summary>
public sealed record BadgeExportRegistrationDto(
    string FirstName,
    string LastName,
    string Email,
    IReadOnlyDictionary<string, string> AdditionalDetails);
