using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings;

internal sealed class GetEventEmailSettingsHandler(IEmailWriteStore writeStore)
    : IQueryHandler<GetEventEmailSettingsQuery, EventEmailSettingsDto?>
{
    public async ValueTask<EventEmailSettingsDto?> HandleAsync(
        GetEventEmailSettingsQuery query,
        CancellationToken cancellationToken)
    {
        var ticketedEventId = TicketedEventId.From(query.TicketedEventId);

        var settings = await writeStore.EventEmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ticketedEventId, cancellationToken);

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
