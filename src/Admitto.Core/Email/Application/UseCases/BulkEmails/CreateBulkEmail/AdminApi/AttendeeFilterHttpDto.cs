using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

/// <summary>
/// HTTP request shape for a bulk-email attendee filter. Bulk email targets
/// registered attendees only; this filter is mapped to the Email-owned
/// <see cref="BulkEmailAttendeeFilter"/> value object.
/// </summary>
public sealed record AttendeeFilterHttpDto(
    IReadOnlyCollection<Guid>? TicketTypeIds = null,
    RegistrationStatus? RegistrationStatus = null,
    bool? HasReconfirmed = null,
    DateTimeOffset? RegisteredAfter = null,
    DateTimeOffset? RegisteredBefore = null,
    IReadOnlyDictionary<string, string>? AdditionalDetailEquals = null)
{
    internal BulkEmailAttendeeFilter ToDomain() =>
        new(
            TicketTypeIds: TicketTypeIds,
            RegistrationStatus: RegistrationStatus,
            HasReconfirmed: HasReconfirmed,
            RegisteredAfter: RegisteredAfter,
            RegisteredBefore: RegisteredBefore,
            AdditionalDetailEquals: AdditionalDetailEquals);
}
