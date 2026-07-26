using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.Extensions.Options;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Email-module-internal contract for resolving deployment SMTP settings.
/// </summary>
internal interface IEffectiveEmailSettingsResolver
{
    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default);

    ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default);
}

internal sealed class EffectiveEmailSettingsResolver(
    IOptions<SystemEmailOptions> options,
    IEmailReadStore readStore) : IEffectiveEmailSettingsResolver
{
    public ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return ResolveAsync(teamId, cancellationToken);
    }

    public async ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.SmtpHost) || string.IsNullOrWhiteSpace(value.FromAddress))
            return null;

        var authMode = ResolveAuthMode(value.AuthMode);

        // Sender identity is entirely deployment configuration (see ADR-013), so the only
        // team-owned fact the send pipeline needs is the accent color. The projection is
        // eventually consistent: a team whose branding event has not reached Email yet has
        // no row at all, which must not block a send — fall back to the default brand color.
        var accentColor = await readStore.TeamEmailContexts
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .Select(c => (AccentColor?)c.AccentColor)
            .SingleOrDefaultAsync(cancellationToken);

        return new EffectiveEmailSettings(
            Hostname.From(value.SmtpHost),
            Port.From(value.SmtpPort),
            value.SmtpSsl,
            value.SmtpStartTls,
            EmailAddress.From(value.FromAddress),
            value.FromDisplayName,
            authMode,
            authMode == EmailAuthMode.Basic ? value.Username : null,
            authMode == EmailAuthMode.Basic ? value.Password : null,
            accentColor ?? AccentColor.From(AccentColor.Default),
            EmailFontFamily.From(EmailFontFamily.Default));
    }

    private static EmailAuthMode ResolveAuthMode(string? value)
    {
        if (bool.TryParse(value, out var enabled))
            return enabled ? EmailAuthMode.Basic : EmailAuthMode.None;

        return Enum.TryParse<EmailAuthMode>(value, ignoreCase: true, out var parsed)
            ? parsed
            : EmailAuthMode.None;
    }
}
