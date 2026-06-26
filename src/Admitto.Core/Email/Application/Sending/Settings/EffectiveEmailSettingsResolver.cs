using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Email.Infrastructure.Security;

namespace Amolenk.Admitto.Core.Email.Application.Sending.Settings;

/// <summary>
/// Email-module-internal contract for resolving team SMTP settings for a given event.
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
    IEmailWriteStore writeStore,
    IProtectedSecret protectedSecret) : IEffectiveEmailSettingsResolver
{
    public async ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        TicketedEventId eventId,
        CancellationToken cancellationToken = default)
    {
        return await ResolveAsync(teamId, cancellationToken);
    }

    public async ValueTask<EffectiveEmailSettings?> ResolveAsync(
        TeamId teamId,
        CancellationToken cancellationToken = default)
    {
        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TeamId == teamId,
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
            password,
            settings.AccentColor,
            settings.FontFamily);
    }
}
