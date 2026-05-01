using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed class CancelRegistrationHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelRegistrationCommand>
{
    public async ValueTask HandleAsync(
        CancelRegistrationCommand command,
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

        registration.Cancel(command.Reason);
    }
}
