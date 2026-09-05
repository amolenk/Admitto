using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

/// <summary>
/// Email-module-owned value object describing which registered attendees a
/// <see cref="Entities.BulkEmailJob"/> targets. It is persisted as the job's
/// durable recipient filter and translated into the Registrations query
/// contract (<c>QueryRegistrationsDto</c>) only transiently at the facade-call
/// boundary in <c>BulkEmailRecipientResolver</c> — the query contract is never
/// part of the Email module's persisted state.
/// </summary>
/// <param name="TicketTypeIds">Optional any-of match against the registration's ticket-type IDs.</param>
/// <param name="RegistrationStatus">Optional registration status filter.</param>
/// <param name="HasReconfirmed">Optional reconfirmation filter.</param>
/// <param name="RegisteredAfter">Optional inclusive lower bound on the registration's creation timestamp.</param>
/// <param name="RegisteredBefore">Optional exclusive upper bound on the registration's creation timestamp.</param>
/// <param name="AdditionalDetailEquals">Optional exact-match filter against the registration's additional details.</param>
/// <param name="RegistrationIds">Optional allowlist of registration IDs (used by system-triggered sends).</param>
public sealed record BulkEmailAttendeeFilter(
    IReadOnlyCollection<Guid>? TicketTypeIds = null,
    RegistrationStatus? RegistrationStatus = null,
    bool? HasReconfirmed = null,
    DateTimeOffset? RegisteredAfter = null,
    DateTimeOffset? RegisteredBefore = null,
    IReadOnlyDictionary<string, string>? AdditionalDetailEquals = null,
    IReadOnlyCollection<Guid>? RegistrationIds = null,
    IReadOnlyDictionary<Guid, Guid>? RegistrationCycleIds = null);
