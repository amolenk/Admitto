using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CancelRegistration;

internal sealed class CancelRegistrationHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<CancelRegistrationCommand>
{
    public async ValueTask HandleAsync(
        CancelRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        RegistrationId registrationId = RegistrationId.From(command.RegistrationId);
        TicketedEventId ticketedEventId = TicketedEventId.From(command.TicketedEventId);

        var registration = await writeStore.Registrations.GetAsync(
                 r => r.Id == registrationId && r.EventId == ticketedEventId,
                 cancellationToken);

        if (command.Reason == CancellationReason.AttendeeRequest)
        {
            var ticketedEvent = await writeStore.TicketedEvents
                .FirstOrDefaultAsync(e => e.Id == ticketedEventId, cancellationToken);

            if (ticketedEvent is not null && DateTimeOffset.UtcNow >= ticketedEvent.StartsAt)
            {
                throw new BusinessRuleViolationException(Errors.EventAlreadyStarted);
            }
        }

        registration.Cancel(command.Reason);
    }

    internal static class Errors
    {
        public static readonly Error EventAlreadyStarted = new(
            "registration.event_already_started",
            "Self-service cancellation is not allowed once the event has started.",
            Type: ErrorType.Conflict);
    }
}
