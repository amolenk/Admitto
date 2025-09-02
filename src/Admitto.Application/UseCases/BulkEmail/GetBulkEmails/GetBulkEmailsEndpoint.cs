namespace Amolenk.Admitto.Application.UseCases.BulkEmail.GetBulkEmails;

/// <summary>
/// Represents an endpoint to get all bulk email jobs.
/// </summary>
public static class GetBulkEmailsEndpoint
{
    public static RouteGroupBuilder MapGetBulkEmails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetBulkEmails)
            .WithName(nameof(GetBulkEmails))
            .RequireAuthorization(policy => policy.RequireCanViewEvent());

        return group;
    }

    private static async ValueTask<Ok<GetBulkEmailsResponse>> GetBulkEmails(
        string teamSlug,
        string eventSlug,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        var workItems = await context.BulkEmailWorkItems
            .Where(wi => wi.TicketedEventId == eventId)
            .ToListAsync(cancellationToken);
            
        var dtos = workItems
            .Select(wi => new BulkEmailWorkItemDto(
                wi.Id,
                wi.EmailType,
                wi.Repeat is null ? null : new BulkEmailWorkItemRepeatDto(
                    wi.Repeat.WindowStart,
                    wi.Repeat.WindowEnd),
                wi.Status,
                wi.LastRunAt,
                wi.Error))
            .ToArray();
        
        return TypedResults.Ok(new GetBulkEmailsResponse(dtos));
    }
}