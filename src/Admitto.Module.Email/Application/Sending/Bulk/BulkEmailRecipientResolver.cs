using System.Text.Json;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Registrations.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.Sending.Bulk;

/// <summary>
/// Materialises a <see cref="BulkEmailJobSource"/> into a frozen recipient
/// snapshot at the start of a bulk-email fan-out (D3 — snapshot-on-resolve).
/// For <see cref="AttendeeSource"/> it queries Registrations via the facade;
/// for <see cref="ExternalListSource"/> it returns the literal items.
/// </summary>
public interface IBulkEmailRecipientResolver
{
    Task<IReadOnlyList<BulkEmailRecipient>> ResolveAsync(
        TicketedEventId eventId,
        BulkEmailJobSource source,
        CancellationToken cancellationToken = default);
}

internal sealed class BulkEmailRecipientResolver(IRegistrationsFacade registrationsFacade)
    : IBulkEmailRecipientResolver
{
    private static readonly JsonSerializerOptions ParametersJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BulkEmailRecipient>> ResolveAsync(
        TicketedEventId eventId,
        BulkEmailJobSource source,
        CancellationToken cancellationToken = default)
    {
        return source switch
        {
            AttendeeSource attendee => await ResolveAttendeesAsync(eventId, attendee, cancellationToken),
            ExternalListSource external => ResolveExternalList(external),
            _ => throw new InvalidOperationException(
                $"Unknown {nameof(BulkEmailJobSource)} type '{source.GetType().Name}'.")
        };
    }

    private async Task<IReadOnlyList<BulkEmailRecipient>> ResolveAttendeesAsync(
        TicketedEventId eventId,
        AttendeeSource source,
        CancellationToken cancellationToken)
    {
        var rows = await registrationsFacade.QueryRegistrationsAsync(
            eventId, source.Filter, cancellationToken);

        var recipients = new List<BulkEmailRecipient>(rows.Count);
        foreach (var row in rows)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["first_name"] = row.FirstName,
                ["last_name"] = row.LastName,
                ["email"] = row.Email,
                ["registration_id"] = row.RegistrationId,
                ["ticket_type_slugs"] = row.TicketTypeSlugs,
                ["additional_details"] = row.AdditionalDetails
            };

            var displayName = string.Concat(row.FirstName, " ", row.LastName).Trim();

            recipients.Add(new BulkEmailRecipient(
                email: row.Email,
                displayName: string.IsNullOrWhiteSpace(displayName) ? null : displayName,
                registrationId: row.RegistrationId,
                parametersJson: JsonSerializer.Serialize(parameters, ParametersJsonOptions)));
        }

        return recipients;
    }

    private static IReadOnlyList<BulkEmailRecipient> ResolveExternalList(ExternalListSource source)
    {
        var recipients = new List<BulkEmailRecipient>(source.Items.Count);
        foreach (var item in source.Items)
        {
            var parameters = new Dictionary<string, object?>
            {
                ["email"] = item.Email,
                ["display_name"] = item.DisplayName
            };

            recipients.Add(new BulkEmailRecipient(
                email: item.Email,
                displayName: item.DisplayName,
                registrationId: null,
                parametersJson: JsonSerializer.Serialize(parameters, ParametersJsonOptions)));
        }

        return recipients;
    }
}
