using Amolenk.Admitto.Application.Common;

namespace Amolenk.Admitto.Application.UseCases.Contributors.UpdateContributor;

/// <summary>
/// Represents the endpoint for updating contributor details.
/// </summary>
public static class UpdateContributorEndpoint
{
    public static RouteGroupBuilder MapUpdateContributor(this RouteGroupBuilder group)
    {
        group
            .MapPatch("/{contributorId:guid}", UpdateContributor)
            .WithName(nameof(UpdateContributor))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> UpdateContributor(
        string teamSlug,
        string eventSlug,
        Guid contributorId,
        UpdateContributorRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var contributor = await context.Contributors.FindAsync([contributorId], cancellationToken);
        if (contributor is null || contributor.TicketedEventId != eventId)
        {
            throw new ApplicationRuleException(ApplicationRuleError.Contributor.NotFound);
        }

        contributor.UpdateDetails(
            request.Email,
            request.FirstName,
            request.LastName,
            (request.AdditionalDetails ?? []).Select(dto =>
                new Domain.ValueObjects.AdditionalDetail(dto.Name, dto.Value)),
            (request.Roles ?? []).Select(dto => Domain.ValueObjects.ContributorRole.Parse(dto.Name)));

        return TypedResults.Ok();
    }
}