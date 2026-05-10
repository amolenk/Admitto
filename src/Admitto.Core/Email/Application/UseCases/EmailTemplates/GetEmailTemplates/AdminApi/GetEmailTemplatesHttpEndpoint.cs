using Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates.AdminApi;

public static class GetEmailTemplatesHttpEndpoint
{
    public static RouteGroupBuilder MapGetEmailTemplates(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "GetTeamEmailTemplates"
            : "GetEventEmailTemplates";

        var handler = new Handler(scope);

        group
            .MapGet("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope)
    {
        public async ValueTask<Ok<IReadOnlyList<EmailTemplateListItemDto>>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            GetEmailTemplatesHandler handler,
            CancellationToken ct)
        {
            var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;
            var parentScopeId = scope == EmailSettingsScope.Event ? teamId : (Guid?)null;

            var rows = await handler.HandleAsync(
                new GetEmailTemplatesQuery(scope, scopeId, parentScopeId), ct);

            return TypedResults.Ok(rows);
        }
    }
}
