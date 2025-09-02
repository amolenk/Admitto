namespace Amolenk.Admitto.Application.UseCases.Email.ScheduleBulkEmail;

/// <summary>
/// Represents an endpoint to schedule a bulk email job.
/// </summary>
public static class ScheduleBulkEmailEndpoint
{
    public static RouteGroupBuilder MapScheduleBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/teams/{teamSlug}/events/{eventSlug}/emails/bulk", ScheduleBulkEmailJob)
            .WithName(nameof(ScheduleBulkEmailJob))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Accepted> ScheduleBulkEmailJob(
        string teamSlug,
        string eventSlug,
        ScheduleBulkEmailRequest request,
        ISlugResolver slugResolver,
        IMessageOutbox outbox,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        var command = new ScheduleBulkEmailCommand(
            teamId,
            eventId,
            request.EmailType,
            request.EarliestSendTime,
            request.LatestSendTime);
        
        outbox.Enqueue(command);

        return TypedResults.Accepted((string?)null);
    }
}