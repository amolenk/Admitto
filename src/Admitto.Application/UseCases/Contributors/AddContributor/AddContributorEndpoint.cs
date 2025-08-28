using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Contributors.AddContributor;

/// <summary>
/// Represents the endpoint for adding a speaker engagement to a ticketed event.
/// </summary>
public static class AddContributorEndpoint
{
    public static RouteGroupBuilder MapAddContributorRegistration(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddSpeakerEngagement)
            .WithName(nameof(AddSpeakerEngagement))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<AddContributorResponse>> AddSpeakerEngagement(
        string teamSlug,
        string eventSlug,
        AddContributorRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var registration = ContributorRegistration.Create(
            teamId,
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList(),
            request.Role);

        context.ContributorRegistrations.Add(registration);

        return TypedResults.Ok(new AddContributorResponse(registration.Id));
    }
}