using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.ReleaseTickets;

internal sealed class ReleaseTicketsHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ReleaseTicketsCommand>
{
    public async ValueTask HandleAsync(
        ReleaseTicketsCommand command,
        CancellationToken cancellationToken)
    {
        var registration = await writeStore.Registrations
            .FirstOrDefaultAsync(
                r => r.Id == command.RegistrationId && r.EventId == command.TicketedEventId,
                cancellationToken);

        if (registration is null)
        {
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Registration>(command.RegistrationId.Value));
        }

        var catalog = await writeStore.TicketCatalogs
            .FirstOrDefaultAsync(
                c => c.Id == command.TicketedEventId,
                cancellationToken);

        if (catalog is null)
            return;

        var ticketSlugs = registration.Tickets.Select(t => t.Slug).ToList();
        catalog.Release(ticketSlugs);
    }
}
