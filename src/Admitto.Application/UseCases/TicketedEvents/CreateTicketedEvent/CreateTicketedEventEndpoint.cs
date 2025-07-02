namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public static class CreateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", CreateTicketedEvent)
            .Produces<Created<CreateTicketedEventResponse>>()
            .ProducesValidationProblem();
        
        return group;
    }

    private static async ValueTask<IResult> CreateTicketedEvent(CreateTicketedEventRequest request,
        CreateTicketedEventValidator validator, IDomainContext context, IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var team = await context.Teams.FindAsync([request.TeamId], cancellationToken);
        if (team is null)
        {
            throw ValidationError.Team.NotFound(request.TeamId);
        }

        var newEvent = request.ToTicketedEvent();
        
        context.TicketedEvents.Add(newEvent);
        
        try
        {        
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw ValidationError.TicketedEvent.AlreadyExists(nameof(request.Name));
        }
        
        return TypedResults.Created($"/events/v1/{newEvent.Id}", 
            CreateTicketedEventResponse.FromTicketedEvent(newEvent));
    }
}
