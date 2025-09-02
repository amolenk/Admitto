using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.ScheduleBulkEmail;

/// <summary>
/// Represents an endpoint to schedule a bulk email job.
/// </summary>
public static class ScheduleBulkEmailEndpoint
{
    public static RouteGroupBuilder MapScheduleBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", ScheduleBulkEmailJob)
            .WithName(nameof(ScheduleBulkEmailJob))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Created> ScheduleBulkEmailJob(
        string teamSlug,
        string eventSlug,
        ScheduleBulkEmailRequest request,
        ISlugResolver slugResolver,
        ScheduleBulkEmailHandler scheduleBulkEmailHandler,
        CancellationToken cancellationToken)
    {
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);

        BulkEmailWorkItemRepeat? repeat = null;
        if (request.Repeat is not null)
        {
            repeat = new BulkEmailWorkItemRepeat(
                request.Repeat.WindowStart,
                request.Repeat.WindowEnd);
        }

        var command = new ScheduleBulkEmailCommand(
            teamId,
            eventId,
            request.EmailType,
            repeat);
        
        await scheduleBulkEmailHandler.HandleAsync(command, cancellationToken);

        return TypedResults.Created();
    }
}