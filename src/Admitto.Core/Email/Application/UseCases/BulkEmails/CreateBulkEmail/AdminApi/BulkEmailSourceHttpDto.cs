using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.BulkEmails.CreateBulkEmail.AdminApi;

/// <summary>
/// HTTP request shape for a bulk-email source. Exactly one of <see cref="Attendee"/>
/// or <see cref="ExternalList"/> must be supplied; the FluentValidation validator
/// enforces that.
/// </summary>
public sealed record BulkEmailSourceHttpDto(
    AttendeeSourceHttpDto? Attendee = null,
    ExternalListSourceHttpDto? ExternalList = null)
{
    internal BulkEmailJobSource ToDomain()
    {
        if (Attendee is not null)
        {
            var filter = new QueryRegistrationsDto(
                TicketTypeIds: Attendee.TicketTypeIds,
                RegistrationStatus: Attendee.RegistrationStatus,
                HasReconfirmed: Attendee.HasReconfirmed,
                RegisteredAfter: Attendee.RegisteredAfter,
                RegisteredBefore: Attendee.RegisteredBefore,
                AdditionalDetailEquals: Attendee.AdditionalDetailEquals);

            return new AttendeeSource(filter);
        }

        if (ExternalList is not null)
        {
            var items = ExternalList.Items
                .Select(i => new ExternalListItem(EmailAddress.From(i.Email), i.DisplayName))
                .ToList();

            return new ExternalListSource(items);
        }

        // Defensive — the validator should have rejected this earlier.
        throw new InvalidOperationException(
            "Bulk-email source must specify exactly one of attendee or externalList.");
    }
}

public sealed record AttendeeSourceHttpDto(
    IReadOnlyCollection<Guid>? TicketTypeIds = null,
    RegistrationStatus? RegistrationStatus = null,
    bool? HasReconfirmed = null,
    DateTimeOffset? RegisteredAfter = null,
    DateTimeOffset? RegisteredBefore = null,
    IReadOnlyDictionary<string, string>? AdditionalDetailEquals = null);

public sealed record ExternalListSourceHttpDto(
    IReadOnlyList<ExternalListRecipientHttpDto> Items);

public sealed record ExternalListRecipientHttpDto(
    string Email,
    string? DisplayName);
