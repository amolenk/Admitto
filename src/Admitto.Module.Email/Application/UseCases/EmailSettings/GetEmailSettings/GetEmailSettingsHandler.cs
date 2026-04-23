using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.GetEmailSettings;

internal sealed class GetEmailSettingsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEmailSettingsQuery, EmailSettingsDto?>
{
    public async ValueTask<EmailSettingsDto?> HandleAsync(GetEmailSettingsQuery query, CancellationToken ct)
    {
        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Scope == query.Scope && s.ScopeId == query.ScopeId, ct);

        if (settings is null)
            return null;

        return new EmailSettingsDto(
            settings.SmtpHost.Value,
            settings.SmtpPort.Value,
            settings.FromAddress.Value,
            settings.AuthMode,
            settings.Username?.Value,
            settings.ProtectedPassword is not null,
            settings.Version);
    }
}
