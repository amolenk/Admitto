using Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks;

public sealed record ScannerLinkDto(
    string Status,
    string? Url,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt);

internal static class ScannerLinkDtoFactory
{
    public static ScannerLinkDto Create(
        TicketedEvent ticketedEvent,
        DateTimeOffset now,
        IOptions<ScannerLinksOptions> options)
    {
        if (ticketedEvent.ScannerLink is null)
            return new ScannerLinkDto("None", null, null, null, null);

        var scannerLink = ticketedEvent.ScannerLink;
        var status = scannerLink.GetStatus(now, ticketedEvent.EndsAt);

        // A link only authorizes anything while its event is Active (see TicketedEvent.EnsureActive
        // gating all scanner-link mutations); once the event is archived, the link is permanently
        // inactive regardless of its own expiry/revocation state, and its secret must not be
        // surfaced even if it still looks "Active" by time/revocation alone.
        if (!ticketedEvent.IsActive && status == ScannerLinkStatus.Active)
            status = ScannerLinkStatus.Expired;

        var secret = status == ScannerLinkStatus.Active
            ? scannerLink.GetVisibleSecret(now, ticketedEvent.EndsAt)
            : null;
        var url = secret is null
            ? null
            : $"{options.Value.BaseUrl.TrimEnd('/')}/scan/{secret.Value.Value}";

        return new ScannerLinkDto(
            status.ToString(),
            url,
            scannerLink.CreatedAt,
            ticketedEvent.EndsAt,
            scannerLink.RevokedAt);
    }
}

