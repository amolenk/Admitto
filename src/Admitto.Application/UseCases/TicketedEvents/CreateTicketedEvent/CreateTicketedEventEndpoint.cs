namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.CreateTicketedEvent;

/// <summary>
/// Create a new ticketed event.
/// </summary>
public static class CreateTicketedEventEndpoint
{
    public static RouteGroupBuilder MapCreateTicketedEvent(this RouteGroupBuilder group)
    {
        group.MapPost("/", CreateTicketedEvent);
        return group;
    }

    private static async ValueTask<Results<Created<CreateTicketedEventResponse>, ValidationProblem, Conflict<HttpValidationProblemDetails>>> CreateTicketedEvent(
        CreateTicketedEventRequest request, CreateTicketedEventValidator validator, IDomainContext context,
        IUnitOfWork unitOfWork, CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        
        var newEvent = request.ToTicketedEvent();

        context.TicketedEvents.Add(newEvent);

        // TODO Check that Scalar also shows BadRequest in OpenAPI docs
        
        try
        {        
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return TypedResults.Conflict(new HttpValidationProblemDetails
            {
                Title = "Conflict",
                Detail = $"An event with the name '{request.Name}' already exists.",
                Status = StatusCodes.Status409Conflict,
                Errors = {
                    ["name"] = ["Event name must be unique."]
                }
            });
        }
        
        return TypedResults.Created($"/events/v1/{newEvent.Id}", 
            CreateTicketedEventResponse.FromTicketedEvent(newEvent));
    }
}