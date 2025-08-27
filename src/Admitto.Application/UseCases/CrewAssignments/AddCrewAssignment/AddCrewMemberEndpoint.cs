using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.CrewAssignments.AddCrewAssignment;

/// <summary>
/// Represents the endpoint for adding a crew member to a ticketed event.
/// </summary>
public static class AddCrewAssignmentEndpoint
{
    public static RouteGroupBuilder MapAddCrewAssignment(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddCrewAssignment)
            .WithName(nameof(AddCrewAssignment))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<AddCrewAssignmentResponse>> AddCrewAssignment(
        string teamSlug,
        string eventSlug,
        AddCrewAssignmentRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.GetTeamAndTicketedEventsIdsAsync(teamSlug, eventSlug, cancellationToken);

        var newAssignment = CrewAssignment.Create(
            teamId,
            eventId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(ad => new AdditionalDetail(ad.Name, ad.Value)).ToList());

        context.CrewAssignments.Add(newAssignment);

        return TypedResults.Ok(new AddCrewAssignmentResponse(newAssignment.Id));
    }
}