namespace Amolenk.Admitto.Application.UseCases.Attendees.CancelRegistration;

/// <summary>
/// Represents the endpoint for cancelling an existing registration for a ticketed eve.
/// </summary>
public static class CancelRegistrationEndpoint
{
    public static RouteGroupBuilder MapCancelRegistration(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{attendeeId:guid}", CancelRegistration)
            .WithName(nameof(CancelRegistration))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> CancelRegistration(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        ISlugResolver slugResolver,
        [FromServices] CancelRegistrationHandler handler,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        await handler.HandleAsync(new CancelRegistrationCommand(eventId, attendeeId), cancellationToken);
        
        return TypedResults.Ok();
    }
}