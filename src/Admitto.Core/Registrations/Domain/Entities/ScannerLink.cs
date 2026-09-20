using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Domain.Entities;

/// <summary>
/// A single shared scanner credential for a <see cref="Entities.TicketedEvent"/>. Owned by the
/// event aggregate; changes are covered by the event's own <c>CreatedAt</c>/<c>LastChangedAt</c>/
/// <c>LastChangedBy</c> audit metadata rather than a dedicated audit trail.
/// </summary>
public class ScannerLink
{
    // EF Core materialization constructor.
    private ScannerLink()
    {
    }

    private ScannerLink(ScannerLinkSecret secret, DateTimeOffset createdAt)
    {
        Secret = secret;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// The recoverable bearer secret while the link is active. Cleared (set to <c>null</c>)
    /// once the link is revoked or regenerated, so a retired secret is never displayed or
    /// returned again.
    /// </summary>
    public ScannerLinkSecret? Secret { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public static ScannerLink Create(ScannerLinkSecret secret, DateTimeOffset now) => new(secret, now);

    /// <summary>
    /// Replaces the current secret with a newly generated one, immediately invalidating the
    /// previous one, and clears any prior revocation.
    /// </summary>
    public void Regenerate(ScannerLinkSecret secret, DateTimeOffset now)
    {
        Secret = secret;
        CreatedAt = now;
        RevokedAt = null;
    }

    /// <summary>
    /// Permanently retires the link. The secret is cleared and can no longer be recovered.
    /// </summary>
    public void Revoke(DateTimeOffset now)
    {
        Secret = null;
        RevokedAt = now;
    }

    public ScannerLinkStatus GetStatus(DateTimeOffset now, DateTimeOffset eventEndsAt)
    {
        if (RevokedAt is not null)
            return ScannerLinkStatus.Revoked;

        if (now >= eventEndsAt)
            return ScannerLinkStatus.Expired;

        return ScannerLinkStatus.Active;
    }

    /// <summary>
    /// Returns the recoverable secret only while the link is Active; expired and revoked
    /// secrets are never surfaced, even if still physically present in storage.
    /// </summary>
    public ScannerLinkSecret? GetVisibleSecret(DateTimeOffset now, DateTimeOffset eventEndsAt) =>
        GetStatus(now, eventEndsAt) == ScannerLinkStatus.Active ? Secret : null;
}
