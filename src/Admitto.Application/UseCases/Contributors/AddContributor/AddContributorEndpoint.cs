using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
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
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok<AddContributorResponse>> AddContributor(
        string teamSlug,
        string eventSlug,
        AddContributorRequest request,
        ISlugResolver slugResolver,
        IApplicationContext context,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        // First get or create a participant. A participant may already exist if the same person is also
        // an attendee of the event.
        var participant = await GetOrCreateParticipantAsync(
            teamId,
            eventId,
            request.Email,
            context,
            unitOfWork,
            cancellationToken);

        var contributor = Contributor.Create(
            eventId,
            participant.Id,
            request.Email,
            request.FirstName,
            request.LastName,
            request.AdditionalDetails.Select(dto => new AdditionalDetail(dto.Name, dto.Value)),
            request.Roles.Select(dto => ContributorRole.Parse(dto.Name)));

        context.Contributors.Add(contributor);

        // Set a more detailed error for unique violation.
        unitOfWork.UniqueViolationError = ApplicationRuleError.Contributor.AlreadyExists;

        return TypedResults.Ok(new AddContributorResponse(contributor.Id));
    }

    private static async ValueTask<Participant> GetOrCreateParticipantAsync(
        Guid teamId,
        Guid eventId,
        string email,
        IApplicationContext context,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var emailNormalized = email.NormalizeEmail();

        var participant = await context.Participants.SingleOrDefaultAsync(
            p => p.TicketedEventId == eventId && p.Email == emailNormalized,
            cancellationToken);

        if (participant is not null) return participant;

        participant = Participant.Create(teamId, eventId, emailNormalized);

        context.Participants.Add(participant);

        await unitOfWork.SaveChangesAsync(
            async () =>
            {
                // Another thread created it first: fetch the existing one.
                participant = await context.Participants.SingleAsync(
                    p => p.TicketedEventId == eventId && p.Email == emailNormalized,
                    cancellationToken);
            },
            cancellationToken);

        return participant;
    }
}