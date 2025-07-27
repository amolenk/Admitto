using Amolenk.Admitto.Domain.Entities;

namespace Amolenk.Admitto.Application.UseCases.Registrations.CompleteRegistration;

/// <summary>
/// Represents a command handler that processes the completion of an attendee's registration for a ticketed event.
/// </summary>
public class CompleteRegistrationHandler(IApplicationContext context) : ICommandHandler<CompleteRegistrationCommand>
{
    public ValueTask HandleAsync(CompleteRegistrationCommand command, CancellationToken cancellationToken)
    {
        var attendee = Registration.Create(
            command.TeamId,
            command.TicketedEventId,
            command.RegistrationId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.AdditionalDetails,
            command.Tickets);

        context.Registrations.Add(attendee);

        return ValueTask.CompletedTask;
    }
}