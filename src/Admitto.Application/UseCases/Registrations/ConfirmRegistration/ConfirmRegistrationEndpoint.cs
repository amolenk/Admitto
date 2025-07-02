using Amolenk.Admitto.Application.UseCases.Attendees.RegisterAttendee;
using Amolenk.Admitto.Application.UseCases.Registrations.StartRegistration;

namespace Amolenk.Admitto.Application.UseCases.Registrations.ConfirmRegistration;


public static class ConfirmRegistrationEndpoint
{
    public static RouteGroupBuilder MapConfirmRegistration(this RouteGroupBuilder group)
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
        RegisterAttendeeValidator validator, IDomainContext context, IMessageOutbox messageOutbox,
        IUnitOfWork unitOfWork, CancellationToken cancellationToken)
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
