using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Email.Infrastructure.Security;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.Settings;

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
                (s.Scope == EmailSettingsScope.Event && s.ScopeId == eventId.Value) ||
                (s.Scope == EmailSettingsScope.Team  && s.ScopeId == teamId.Value))
            .ToListAsync(cancellationToken);

        var effective = settings.FirstOrDefault(s => s.Scope == EmailSettingsScope.Event)
                     ?? settings.FirstOrDefault(s => s.Scope == EmailSettingsScope.Team);

        return effective is null ? null : ToEffective(effective);
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
