namespace Amolenk.Admitto.Module.Registrations.Contracts;

/// <summary>
/// Reusable filter shape for cross-module registration queries (e.g. used by the
/// Email module to resolve bulk-email recipients via
/// <see cref="IRegistrationsFacade.QueryRegistrationsAsync"/>). All filters are
/// optional; an empty <see cref="QueryRegistrationsDto"/> matches every
/// registration on the target ticketed event.
/// </summary>
/// <param name="TicketTypeSlugs">
/// Optional any-of match against the slugs of the ticket types held by the
/// registration. A registration matches when at least one of its ticket
/// snapshots has a slug in this set.
/// </param>
/// <param name="RegistrationStatus">Optional registration status filter.</param>
/// <param name="HasReconfirmed">
/// Optional reconfirmation filter. <c>true</c> matches registrations that have
/// reconfirmed; <c>false</c> matches registrations that have not.
/// </param>
/// <param name="RegisteredAfter">
/// Optional inclusive lower bound on the registration's creation timestamp.
/// </param>
/// <param name="RegisteredBefore">
/// Optional exclusive upper bound on the registration's creation timestamp.
/// </param>
/// <param name="AdditionalDetailEquals">
/// Optional equality filter against entries in the registration's
/// <c>AdditionalDetails</c> map. Each key/value pair must match exactly.
/// </param>
public sealed record QueryRegistrationsDto(
    IReadOnlyCollection<string>? TicketTypeSlugs = null,
    RegistrationStatus? RegistrationStatus = null,
    bool? HasReconfirmed = null,
    DateTimeOffset? RegisteredAfter = null,
    DateTimeOffset? RegisteredBefore = null,
    IReadOnlyDictionary<string, string>? AdditionalDetailEquals = null);
