using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.IntegrationEvents;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.RequestTicketConfirmationResend;

internal sealed class RequestTicketConfirmationResendHandler(
    IRegistrationsWriteStore writeStore,
    [FromKeyedServices(RegistrationsModule.Key)] IOutbox outbox)
    : ICommandHandler<RequestTicketConfirmationResendCommand>
{
    public async ValueTask HandleAsync(
        RequestTicketConfirmationResendCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var eventId = TicketedEventId.From(command.TicketedEventId);
        var registrationId = RegistrationId.From(command.RegistrationId);

        var registration = await writeStore.Registrations
            .Where(r => r.TeamId == teamId
                        && r.EventId == eventId
                        && r.Id == registrationId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (registration is null)
            throw new BusinessRuleViolationException(NotFoundError.Create<Registration>());

        if (registration.Status != RegistrationStatus.Registered)
            throw new BusinessRuleViolationException(Errors.RegistrationNotRegistered);

        outbox.Enqueue(new TicketConfirmationResendRequestedIntegrationEvent(
            TeamId: command.TeamId,
            TicketedEventId: command.TicketedEventId,
            RegistrationId: registration.Id.Value,
            ResendRequestId: command.ResendRequestId,
            RecipientEmail: registration.Email.Value,
            FirstName: registration.FirstName.Value,
            LastName: registration.LastName.Value,
            TicketNames: registration.Tickets.Select(t => t.Name.Value).ToList()));
    }

    internal static class Errors
    {
        public static readonly Error RegistrationNotRegistered = new(
            "registration.not_registered",
            "Only registered attendees can receive ticket-confirmation resends.",
            Type: ErrorType.Conflict);
    }
}
