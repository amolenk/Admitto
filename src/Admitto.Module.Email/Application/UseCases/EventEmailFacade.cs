using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Email.Application.UseCases;

internal sealed class EventEmailFacade(IEmailWriteStore writeStore) : IEventEmailFacade
{
    public async ValueTask<bool> IsEmailConfiguredAsync(
        TicketedEventId ticketedEventId,
        CancellationToken cancellationToken = default)
    {
        var settings = await writeStore.EventEmailSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ticketedEventId, cancellationToken);

        return settings?.IsValid() == true;
    }
}
