using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Attendees.ReconfirmRegistration;

/// <summary>
/// Represents the endpoint for cancelling an existing registration for a ticketed eve.
/// </summary>
public static class ReconfirmRegistrationEndpoint
{
    public static RouteGroupBuilder MapReconfirmRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/{attendeeId:guid}/reconfirm", ReconfirmRegistration)
            .WithName(nameof(ReconfirmRegistration))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> ReconfirmRegistration(
        string teamSlug,
        string eventSlug,
        Guid attendeeId,
        ISlugResolver slugResolver,
        [FromServices] ReconfirmRegistrationHandler handler,
        CancellationToken cancellationToken)
    {
        var eventId= await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        await handler.HandleAsync(new ReconfirmRegistrationCommand(eventId, attendeeId), cancellationToken);
        
        return TypedResults.Ok();
    }
}