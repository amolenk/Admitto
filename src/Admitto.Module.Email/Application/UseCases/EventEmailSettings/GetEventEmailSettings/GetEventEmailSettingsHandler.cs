using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings;

internal sealed class GetEventEmailSettingsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEventEmailSettingsQuery, EventEmailSettingsDto?>
{
    public async ValueTask<EventEmailSettingsDto?> HandleAsync(
        GetEventEmailSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var eventId = query.TicketedEventId;

        var settings = await writeStore.EmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.Scope == EmailSettingsScope.Event && s.ScopeId == eventId,
                cancellationToken);

        if (settings is null)
            return null;

        return new EventEmailSettingsDto(
            settings.SmtpHost.Value,
            settings.SmtpPort.Value,
            settings.FromAddress.Value,
            settings.AuthMode,
            settings.Username?.Value,
            settings.ProtectedPassword is not null,
            settings.Version);
    }
}
