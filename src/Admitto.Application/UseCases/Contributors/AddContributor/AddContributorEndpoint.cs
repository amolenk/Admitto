using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Application.Projections.Participation;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Contributors.AddContributor;

/// <summary>
/// Represents the endpoint for adding a contributor to a ticketed event.
/// </summary>
public static class AddContributorEndpoint
{
    public static RouteGroupBuilder MapAddContributor(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddContributor)
            .WithName(nameof(AddContributor))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<AddContributorResponse>> AddContributor(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromBody] AddContributorRequest request,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        [FromServices] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        // First get or create a participant. A participant may already exist if the same person is also
        // an attendee of the event.
        var participantId = await GetOrCreateParticipant(teamId, eventId, request, context, cancellationToken);

        var contributorId = CreateContributor(eventId, participantId, request, context);

        // Handle unique violations that may occur due to concurrent requests.
        unitOfWork.OnUniqueViolation = args =>
        {
            // If we fail because some other thread created the participant first, we should consider that an
            // optimistic concurrency failure and retry the entire operation.
            if (args.Error == ApplicationRuleError.Participant.AlreadyExists)
            {
                args.Retry = true;
            }
        };

        return TypedResults.Ok(new AddContributorResponse(contributorId));
    }

    private static async ValueTask<Guid> GetOrCreateParticipant(
        Guid teamId,
        Guid eventId,
        AddContributorRequest request,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        // First check if the participant already exists. The contributor could already be an attendee of the event.
        var existingParticipant = await context.ParticipationView
            .Where(p => p.TicketedEventId == eventId && p.Email == request.Email)
            .Select(p => new
            {
                p.ParticipantId,
                p.ContributorStatus
            })
            .FirstOrDefaultAsync(cancellationToken);

        Guid participantId;
        if (existingParticipant is not null)
        {
            // If the participant is already an active contributor, we can stop here.
            if (existingParticipant.ContributorStatus == ParticipationContributorStatus.Active)
            {
                throw new ApplicationRuleException(ApplicationRuleError.Contributor.AlreadyExists);
            }

            participantId = existingParticipant.ParticipantId;
        }
        else
        {
            // Create a new participant.
            var newParticipant = Participant.Create(teamId, eventId, request.Email);
            context.Participants.Add(newParticipant);

            participantId = newParticipant.Id;
        }

        return participantId;
    }

    private static Guid CreateContributor(
        Guid eventId,
        Guid participantId,
        AddContributorRequest request,
        IApplicationContext context)
    {
        var id = Guid.NewGuid();

        var contributor = Contributor.Create(
            id,
            eventId,
            participantId,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(dto => new AdditionalDetail(dto.Name, dto.Value)),
            request.Roles);

        context.Contributors.Add(contributor);

        return id;
    }
}