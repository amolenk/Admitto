using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed class CancelRegistrationHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelRegistrationCommand>
{
    public async ValueTask HandleAsync(
        CancelRegistrationCommand command,
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

        registration.Cancel(command.Reason);
    }
}
