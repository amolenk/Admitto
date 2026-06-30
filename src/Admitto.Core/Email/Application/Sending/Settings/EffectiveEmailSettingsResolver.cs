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

        var teamContext = await readStore.TeamEmailContexts
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .Select(c => new
            {
                c.TeamName,
                c.ReplyToEmailAddress
            })
            .SingleOrDefaultAsync(cancellationToken);

        var fromAddress = EmailAddress.From(value.FromAddress);
        var fromDisplayName = string.IsNullOrWhiteSpace(teamContext?.TeamName)
            ? fromAddress.Value
            : teamContext.TeamName;

        return new EffectiveEmailSettings(
            Hostname.From(value.SmtpHost),
            Port.From(value.SmtpPort),
            value.SmtpSsl,
            value.SmtpStartTls,
            fromAddress,
            fromDisplayName,
            teamContext?.ReplyToEmailAddress,
            authMode,
            authMode == EmailAuthMode.Basic ? value.Username : null,
            authMode == EmailAuthMode.Basic ? value.Password : null,
            EmailAccentColor.From("#2563eb"),
            EmailFontFamily.From("Inter, sans-serif"));
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
