namespace Amolenk.Admitto.Application.UseCases.Contributors.GetContributors;

public static class GetContributorsEndpoint
{
    public static RouteGroupBuilder MapGetContributors(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetContributors)
            .WithName(nameof(GetContributors))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetContributorsResponse>> GetContributors(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var contributors = await context.Contributors
            .AsNoTracking()
            .Where(cr => cr.TicketedEventId == eventId)
            .Select(cr => new ContributorDto(
                cr.Id,
                cr.Email,
                cr.FirstName,
                cr.LastName,
                cr.Roles.ToArray(),
                cr.LastChangedAt))
            .ToArrayAsync(cancellationToken);

        return TypedResults.Ok(new GetContributorsResponse(contributors));
    }
}