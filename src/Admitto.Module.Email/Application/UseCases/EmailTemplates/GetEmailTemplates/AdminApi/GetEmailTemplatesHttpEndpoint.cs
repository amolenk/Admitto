using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplates.AdminApi;

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
            IMediator mediator,
            CancellationToken ct)
        {
            var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;
            var parentScopeId = scope == EmailSettingsScope.Event ? teamId : (Guid?)null;

            var rows = await mediator.QueryAsync<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplateListItemDto>>(
                new GetEmailTemplatesQuery(scope, scopeId, parentScopeId), ct);

            return TypedResults.Ok(rows);
        }
    }
}
