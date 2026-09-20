using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;

/// <summary>
/// Resolves and validates a shared scanner link secret to its owning, currently
/// authorized <see cref="TicketedEvent"/>. This is the single seam every anonymous
/// shared-scanner use case goes through, so malformed credentials, wrong-event
/// attempts, inactive events, and expired/revoked/replaced secrets are all rejected
/// the same way and never distinguished to the caller (see <see cref="Errors.AccessDenied"/>).
/// </summary>
internal static class SharedScannerAccess
{
    public static async ValueTask<TicketedEvent> ResolveActiveEventAsync(
        IRegistrationsWriteStore writeStore,
        string secret,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secret))
            throw new BusinessRuleViolationException(Errors.AccessDenied);

        ScannerLinkSecret parsedSecret;
        try
        {
            parsedSecret = ScannerLinkSecret.From(secret);
        }
        catch (Exception)
        {
            throw new BusinessRuleViolationException(Errors.AccessDenied);
        }

        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.ScannerLink != null && e.ScannerLink.Secret == parsedSecret,
                cancellationToken);

        if (ticketedEvent is null || !ticketedEvent.IsActive)
            throw new BusinessRuleViolationException(Errors.AccessDenied);

        var status = ticketedEvent.ScannerLink!.GetStatus(now, ticketedEvent.EndsAt);
        if (status != ScannerLinkStatus.Active)
            throw new BusinessRuleViolationException(Errors.AccessDenied);

        return ticketedEvent;
    }

    internal static class Errors
    {
        public static readonly Error AccessDenied = new(
            "shared_scanner.access_denied",
            "This scanner link is no longer valid. Contact the event organizer for access.",
            Type: ErrorType.Unauthorized);
    }
}
