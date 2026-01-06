using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailRecipientLists.GetEmailRecipientLists;

/// <summary>
/// Represents the endpoint for retrieving email recipient lists.
/// </summary>
public static class GetEmailRecipientListsEndpoint
{
    public static RouteGroupBuilder MapGetEmailRecipientLists(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetEmailRecipientLists)
            .WithName(nameof(GetEmailRecipientLists))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<GetEmailRecipientListsResponse>> GetEmailRecipientLists(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var names = await context.EmailRecipientLists
            .AsNoTracking()
            .Where(erl => erl.TicketedEventId == eventId)
            .Select(erl => erl.Name)
            .ToArrayAsync(cancellationToken);
        
        return TypedResults.Ok(new GetEmailRecipientListsResponse(names));
    }
}