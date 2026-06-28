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

        var authMode = Enum.TryParse<EmailAuthMode>(value.AuthMode, ignoreCase: true, out var parsed)
            ? parsed
            : EmailAuthMode.None;

        var replyToAddress = await readStore.TeamEmailContexts
            .AsNoTracking()
            .Where(c => c.TeamId == teamId)
            .Select(c => c.ReplyToEmailAddress)
            .SingleOrDefaultAsync(cancellationToken);

        return new EffectiveEmailSettings(
            Hostname.From(value.SmtpHost),
            Port.From(value.SmtpPort),
            EmailAddress.From(value.FromAddress),
            replyToAddress,
            authMode,
            authMode == EmailAuthMode.Basic ? value.Username : null,
            authMode == EmailAuthMode.Basic ? value.Password : null,
            EmailAccentColor.From("#2563eb"),
            EmailFontFamily.From("Inter, sans-serif"));
    }
}
