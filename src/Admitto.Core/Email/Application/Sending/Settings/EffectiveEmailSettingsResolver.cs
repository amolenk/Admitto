using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Email-module-internal contract for resolving effective SMTP settings for a given event,
/// falling back to team-scoped settings when no event-scoped settings exist.
/// </summary>
internal interface IEffectiveEmailSettingsResolver
{
    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves effective SMTP settings for a team scope (no event-level fallback).
    /// </summary>
    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);
}

internal sealed class EffectiveEmailSettingsResolver(
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret) : IEffectiveEmailSettingsResolver
{
    public async ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        // Try event-scoped first, then fall back to team-scoped.
        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .Where(s =>
                s.TeamId == teamId &&
                (s.TicketedEventId == eventId || s.TicketedEventId == null))
            .ToListAsync(cancellationToken);

        var effective = settings.FirstOrDefault(s => s.TicketedEventId == eventId)
                     ?? settings.FirstOrDefault(s => s.TicketedEventId == null);

        return effective is null ? null : ToEffective(effective);
    }

    public async ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TeamId == teamId &&
                     s.TicketedEventId == null,
                cancellationToken);

        return settings is null ? null : ToEffective(settings);
    }

    private EffectiveEmailSettings ToEffective(EmailSettings settings)
    {
        var password = settings.ProtectedPassword is null
            ? null
            : protectedSecret.Unprotect(settings.ProtectedPassword.Value.Ciphertext);

        return new EffectiveEmailSettings(
            settings.SmtpHost,
            settings.SmtpPort,
            settings.FromAddress,
            settings.AuthMode,
            settings.Username?.Value,
            password);
    }
}
