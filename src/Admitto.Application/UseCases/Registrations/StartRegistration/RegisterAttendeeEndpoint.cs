namespace Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

/// <summary>
/// The registration flow optimistically adds a new registration.
/// The flow includes checks for event capacity and whether the attendee already has a registration.
/// However, there's a race condition where the event fills up while we're executing the registration flow.
/// Therefore, an asynchronous ReserveTicketsCommand is published to really check event capacity and finalize ticket
/// reservations.
/// </summary>
public static class RegisterAttendeeEndpoint
{
    public static RouteGroupBuilder MapRegisterAttendee(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", RegisterAttendee)
            .WithName(nameof(RegisterAttendee))
            .Produces<Created<RegisterAttendeeResponse>>()
            .Produces<ValidationProblem>();
//            .RequireAuthorization(policy => policy.RequireRebacCheck(Permission.CanManageTeams));;
        
        return group;
    }

    private static async ValueTask<IResult> RegisterAttendee(RegisterAttendeeRequest request, 
        RegisterAttendeeValidator validator, IDomainContext context, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var ticketedEvent = await context.TicketedEvents.FindAsync([request.TicketedEventId],
            cancellationToken);
        if (ticketedEvent is null)
        {
            throw ValidationError.TicketedEvent.NotFound(request.TicketedEventId);
        }

        var registration = request.ToAttendeeRegistration();
        
        // Early exit: If there's not enough capacity, reject immediately.
        if (!ticketedEvent.HasAvailableCapacity(registration.Tickets))
        {
            throw ValidationError.TicketedEvent.SoldOut();
        }
        
        // Optimistically add a new registration.
        context.AttendeeRegistrations.Add(registration);

        // TODO Create alternative flow for users that are already registered.

        // TODO Move to middleware if we can get away with generic error messages.
        try
        {        
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ValidationError.AttendeeRegistration.AlreadyExists();
        }
        
        
        return TypedResults.Created($"/registrations/{registration.Id}",
            RegisterAttendeeResponse.FromAttendeeRegistration(registration));
    }
}
