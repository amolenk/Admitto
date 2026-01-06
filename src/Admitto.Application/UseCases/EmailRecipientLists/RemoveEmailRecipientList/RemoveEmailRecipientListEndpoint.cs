using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailRecipientLists.RemoveEmailRecipientList;

/// <summary>
/// Represents the endpoint for removing an email recipient list.
/// </summary>
public static class RemoveEmailRecipientListEndpoint
{
    public static RouteGroupBuilder MapRemoveEmailRecipientList(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{name}", RemoveEmailRecipientList)
            .WithName(nameof(RemoveEmailRecipientList))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> RemoveEmailRecipientList(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromRoute] string name,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var emailRecipientList = await context.EmailRecipientLists
            .Where(erl => erl.TicketedEventId == eventId && erl.Name == name)
            .FirstOrDefaultAsync(cancellationToken);

        if (emailRecipientList is null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailRecipientList.NotFound);
        }
        
        context.EmailRecipientLists.Remove(emailRecipientList);

        return TypedResults.Ok();
    }
}