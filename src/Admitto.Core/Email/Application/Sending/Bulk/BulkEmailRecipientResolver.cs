using System.Text.Json;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Bulk;

/// <summary>
/// Materialises a <see cref="BulkEmailAttendeeFilter"/> into a frozen recipient
/// snapshot at the start of a bulk-email fan-out (D3 — snapshot-on-resolve).
/// The Email-owned filter is translated into the Registrations query contract
/// (<see cref="QueryRegistrationsDto"/>) only here, at the facade-call boundary,
/// and resolved via <c>IRegistrationsFacade.GetRegistrationsAsync</c>.
/// </summary>
public interface IBulkEmailRecipientResolver
{
    Task<IReadOnlyList<BulkEmailRecipient>> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        BulkEmailAttendeeFilter attendeeFilter,
        CancellationToken cancellationToken = default);
}

internal sealed class BulkEmailRecipientResolver(IRegistrationsFacade registrationsFacade)
    : IBulkEmailRecipientResolver
{
    private static readonly JsonSerializerOptions ParametersJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BulkEmailRecipient>> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        BulkEmailAttendeeFilter attendeeFilter,
        CancellationToken cancellationToken = default)
    {
        var query = new QueryRegistrationsDto(
            TicketTypeIds: attendeeFilter.TicketTypeIds,
            RegistrationStatus: attendeeFilter.RegistrationStatus,
            HasReconfirmed: attendeeFilter.HasReconfirmed,
            RegisteredAfter: attendeeFilter.RegisteredAfter,
            RegisteredBefore: attendeeFilter.RegisteredBefore,
            AdditionalDetailEquals: attendeeFilter.AdditionalDetailEquals,
            RegistrationIds: attendeeFilter.RegistrationIds);

        var rows = await registrationsFacade.GetRegistrationsAsync(
            teamId.Value, eventId.Value, query, cancellationToken);

        var recipients = new List<BulkEmailRecipient>(rows.Count);
        foreach (var row in rows)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["first_name"] = row.FirstName,
                ["last_name"] = row.LastName,
                ["email"] = row.Email,
                ["registration_id"] = row.RegistrationId,
                ["ticket_type_ids"] = row.TicketTypeIds,
                ["additional_details"] = row.AdditionalDetails
            };

            var displayName = string.Concat(row.FirstName, " ", row.LastName).Trim();

            recipients.Add(new BulkEmailRecipient(
                email: EmailAddress.From(row.Email),
                displayName: displayName,
                registrationId: RegistrationId.From(row.RegistrationId),
                parametersJson: JsonSerializer.Serialize(parameters, ParametersJsonOptions)));
        }

        return recipients;
    }
}
