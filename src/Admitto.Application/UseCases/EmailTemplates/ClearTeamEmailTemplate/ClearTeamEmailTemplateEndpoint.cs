using Amolenk.Admitto.Application.Common.Persistence;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.UseCases.EmailTemplates.ClearTeamEmailTemplate;

/// <summary>
/// Represents an endpoint for clearing an email template for a specific team.
/// </summary>
public static class ClearTeamEmailTemplateEndpoint
{
    public static RouteGroupBuilder MapClearTeamEmailTemplate(this RouteGroupBuilder group)
    {
        group
            .MapDelete("/email-templates/{emailType}", ClearTeamEmailTemplate)
            .WithName(nameof(ClearTeamEmailTemplate))
            .RequireAuthorization(policy => policy.RequireTeamMemberRole(TeamMemberRole.Owner));

        return group;
    }

    private static async ValueTask<Ok> ClearTeamEmailTemplate(
        string teamSlug,
        string emailType,
        ISlugResolver slugResolver,
        IApplicationContext context,
        CancellationToken cancellationToken)
    {
        var teamId = await slugResolver.ResolveTeamIdAsync(teamSlug, cancellationToken);

        await context.EmailTemplates
            .Where(t => t.TeamId == teamId && t.TicketedEventId == null
                && t.Type == emailType)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}