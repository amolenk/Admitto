using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.BulkEmail.RemoveBulkEmail;

/// <summary>
/// Represents an endpoint to remove a scheduled bulk email job.
/// </summary>
public static class RemoveBulkEmailEndpoint
{
    public static RouteGroupBuilder MapRemoveBulkEmail(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/{workItemId:guid}", RemoveBulkEmailJob)
            .WithName(nameof(RemoveBulkEmailJob))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> RemoveBulkEmailJob(
        string teamSlug,
        string eventSlug,
        Guid workItemId,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var eventId = await slugResolver.ResolveTicketedEventIdAsync(teamSlug, eventSlug, cancellationToken);

        // TODO Are there any negative consequences to using SingleOrDefaultAsync in general with EF Core?
        var workItem = await context.BulkEmailWorkItems
            .Where(wi => wi.Id == workItemId && wi.TicketedEventId == eventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (workItem == null)
        {
            throw new ApplicationRuleException(ApplicationRuleError.BulkEmail.WorkItemNotFound);
        }

        if (workItem.Status == BulkEmailWorkItemStatus.Running)
        {
            throw new ApplicationRuleException(
                ApplicationRuleError.BulkEmail.CannotRemoveWorkItemInStatus(workItem.Status));
        }
        
        context.BulkEmailWorkItems.Remove(workItem);

        return TypedResults.Ok();
    }
}