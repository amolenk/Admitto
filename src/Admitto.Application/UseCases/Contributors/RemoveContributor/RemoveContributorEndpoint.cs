using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Contributors.RemoveContributor;

/// <summary>
/// Represents the endpoint for removing a contributor of a ticketed event.
/// </summary>
public static class RemoveContributorEndpoint
{
    public static RouteGroupBuilder MapRemoveContributor(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{contributorId:guid}", RemoveContributor)
            .WithName(nameof(RemoveContributor))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> RemoveContributor(
        string teamSlug,
        string eventSlug,
        Guid contributorId,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var contributor = await context.Contributors.FindAsync([contributorId], cancellationToken);
        if (contributor is null || contributor.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Contributor.NotFound);
        }

        contributor.MarkAsRemoved();

        context.Contributors.Remove(contributor);

        return TypedResults.Ok();
    }
}