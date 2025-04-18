using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;

/// <summary>
/// The registration flow optimistically adds a new registration.
/// The flow includes checks for event capacity and whether the attendee
/// already has a registration.
/// However, there's a race condition where the event fills up while we're
/// executing the registration flow. Therefore, an asynchronous ReserveTicketsCommand
/// is published to check event capacity and finalize ticket reservations.
/// </summary>
public class RegisterAttendeeHandler(IDomainContext context, IMessageOutbox messageOutbox)
    : ICommandHandler<RegisterAttendeeCommand, Guid>
{
    public async ValueTask<Result<Guid>> HandleAsync(RegisterAttendeeCommand command, CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([command.TeamId], cancellationToken);
        if (team is null)
        {
            return Result<Guid>.Failure(Error.TeamNotFound(command.TeamId));
        }

        var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == command.TicketedEventId);
        if (ticketedEvent is null)
        {
            return Result<Guid>.Failure(Error.TicketedEventNotFound(command.TicketedEventId));
        }
        
        var ticketOrder = TicketOrder.Create(command.TicketTypes);
        
        // Early exit: If there's not enough capacity, reject immediately.
        if (!ticketedEvent.HasAvailableCapacity(ticketOrder))
        {
            return Result<Guid>.Failure(Error.InsufficientCapacity);
        }

        // Optimistically add a new registration.
        var registration = AttendeeRegistration.Create( command.TeamId,command.TicketedEventId, command.Email,
            command.FirstName, command.LastName, command.OrganizationName, ticketOrder);
        //
        context.AttendeeRegistrations.Add(registration);
        
        // Add a command to the outbox to reserve the tickets asynchronously.
        // At this point, everything looks ok, but we can't be 100% sure the event isn't full.
        messageOutbox.EnqueueCommand(new ReserveTicketsCommand(registration.Id));

        return Result<Guid>.Success(registration.Id);
    }
}
