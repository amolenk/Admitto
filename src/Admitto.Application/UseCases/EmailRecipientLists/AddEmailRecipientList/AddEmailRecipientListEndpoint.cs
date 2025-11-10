using Amolenk.Admitto.Application.Common;
using Amolenk.Admitto.Application.Common.Email;
using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;
using Humanizer;

namespace Amolenk.Admitto.Application.UseCases.EmailRecipientLists.AddEmailRecipientList;

/// <summary>
/// Represents the endpoint for adding an email recipient list.
/// </summary>
public static class AddEmailRecipientListEndpoint
{
    public static RouteGroupBuilder MapAddEmailRecipientList(this RouteGroupBuilder group)
    {
        group
            .MapPost("/", AddEmailRecipientList)
            .WithName(nameof(AddEmailRecipientList))
            .RequireAuthorization(policy => policy.RequireCanUpdateEvent());

        return group;
    }

    private static async ValueTask<Ok> AddEmailRecipientList(
        [FromRoute] string teamSlug,
        [FromRoute] string eventSlug,
        [FromBody] AddEmailRecipientListRequest request,
        [FromServices] ISlugResolver slugResolver,
        [FromServices] IApplicationContext context,
        CancellationToken cancellationToken)
    {
        if (request.Recipients.Length == 0)
        {
            throw new ApplicationRuleException(ApplicationRuleError.EmailRecipientList.NotFound);
        }
        
        var (teamId, eventId) =
            await slugResolver.ResolveTeamAndTicketedEventIdsAsync(teamSlug, eventSlug, cancellationToken);
        
        var emailRecipientList = EmailRecipientList.Create(
            eventId,
            request.Name,
            request.Recipients.Select(r => new EmailRecipient
            {
                Email = r.Email.NormalizeEmail(),
                Details = r.Details
                    .Select(d => new EmailRecipientDetail(d.Name.Underscore(), d.Value))
                    .ToList()
            }));
        
        context.EmailRecipientLists.Add(emailRecipientList);

        return TypedResults.Ok();
    }
}