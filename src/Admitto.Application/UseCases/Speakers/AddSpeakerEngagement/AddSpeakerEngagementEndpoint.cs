using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Speakers.AddSpeakerEngagement;

/// <summary>
/// Represents the endpoint for adding a speaker engagement to a ticketed event.
/// </summary>
public static class AddSpeakerEngagementEndpoint
{
    public static RouteGroupBuilder MapAddSpeakerEngagement(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddSpeakerEngagement)
            .WithName(nameof(AddSpeakerEngagement))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<AddSpeakerEngagementResponse>> AddSpeakerEngagement(
        string teamSlug,
        string eventSlug,
        AddSpeakerEngagementRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var newEngagement = SpeakerEngagement.Create(
            teamId,
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList());

        context.SpeakerEngagements.Add(newEngagement);

        return TypedResults.Ok(new AddSpeakerEngagementResponse(newEngagement.Id));
    }
}