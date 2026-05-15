using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed class ReleaseTicketsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ReleaseTicketsCommand>
{
    public async ValueTask HandleAsync(
        ReleaseTicketsCommand command,
        CancellationToken cancellationToken)
    {
        RegistrationId registrationId = RegistrationId.From(command.RegistrationId);
        TicketedEventId ticketedEventId = TicketedEventId.From(command.TicketedEventId);

        var registration = await writeStore.Registrations
            .FirstOrDefaultAsync(
                r => r.Id == registrationId && r.EventId == ticketedEventId,
                cancellationToken);

        if (registration is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Registration>(registrationId.Value));
        }

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(
                c => c.Id == ticketedEventId,
                cancellationToken);

        if (catalog is null)
            return;

        var ticketIds = registration.Tickets.Select(t => t.Id).ToList();
        catalog.Release(ticketIds);
    }
}
