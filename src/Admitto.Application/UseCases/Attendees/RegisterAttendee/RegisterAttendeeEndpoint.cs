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
public static class RegisterAttendeeEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
    {
        group.MapPost("/", RegisterAttendee);
        return group;
    }

    private static async ValueTask<Results<Created<RegisterAttendeeResponse>, BadRequest<string>>> RegisterAttendee(Guid teamId,
        Guid ticketedEventId, RegisterAttendeeRequest request, IDomainContext context, IMessageOutbox messageOutbox,
        CancellationToken cancellationToken)
    {
        var team = await context.Teams.FindAsync([teamId], cancellationToken);
        if (team is null)
        {
            return TypedResults.BadRequest(Error.TeamNotFound(teamId));
        }

        var ticketedEvent = team.ActiveEvents.FirstOrDefault(e => e.Id == ticketedEventId);
        if (ticketedEvent is null)
        {
            return TypedResults.BadRequest(Error.TicketedEventNotFound(ticketedEventId));
        }
        
        var ticketOrder = TicketOrder.Create(request.TicketTypes);
        
        // Early exit: If there's not enough capacity, reject immediately.
        if (!ticketedEvent.HasAvailableCapacity(ticketOrder))
        {
            return TypedResults.BadRequest(Error.InsufficientCapacity);
        }

        // Optimistically add a new registration.
        var registration = AttendeeRegistration.Create(teamId,ticketedEventId, request.Email,
            request.FirstName, request.LastName, request.OrganizationName, ticketOrder);
        //
        // TODO What happens if the registration already exists?
        // TODO Maybe automatic Conflict error is OK?
        context.AttendeeRegistrations.Add(registration);
        
        // Add a command to the outbox to reserve the tickets asynchronously.
        // At this point, everything looks ok, but we can't be 100% sure the event isn't full.
        messageOutbox.EnqueueCommand(new ReserveTicketsCommand(registration.Id));

        return TypedResults.Created($"/teams/{teamId}/events/{ticketedEventId}/registrations/{registration.Id}",
            RegisterAttendeeResponse.FromAttendeeRegistration(registration));
    }
}
