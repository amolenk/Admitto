using Amolenk.Admitto.Application.Common.Authorization;
using Amolenk.Admitto.Application.UseCases.Email.ConfigureEventEmailTemplate;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.Email.ClearTeamEmailTemplate;

public static class ClearTeamEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapClearTeamEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/teams/{teamSlug}/email/templates/{emailType}", ClearTeamEmailTemplate)
            .WithName(nameof(ClearTeamEmailTemplate))
            .RequireAuthorization(policy => policy.RequireCanUpdateTeam());

        return group;
    }

    private static async ValueTask<Ok> ClearTeamEmailTemplate(
        string teamSlug,
        EmailType emailType,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.GetTeamIdAsync(teamSlug, cancellationToken);

        await context.EmailTemplates
            .Where(t => t.TeamId == teamId && t.TicketedEventId == null
                && t.Type == emailType)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}