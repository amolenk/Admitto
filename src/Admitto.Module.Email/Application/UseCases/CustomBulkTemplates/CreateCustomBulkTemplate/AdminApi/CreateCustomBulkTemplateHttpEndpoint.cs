using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate.AdminApi;

public static class CreateCustomBulkTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapCreateCustomBulkTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope,
        Func<OrganizationScope, Guid> scopeIdSelector)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "CreateTeamCustomBulkTemplate"
            : "CreateEventCustomBulkTemplate";

        var handler = new Handler(scope, scopeIdSelector);

        group
            .MapPost("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope, Func<OrganizationScope, Guid> scopeIdSelector)
    {
        public async ValueTask<Created<CreateCustomBulkTemplateResponse>> HandleAsync(
            string teamSlug,
            string? eventSlug,
            CreateCustomBulkTemplateHttpRequest request,
            IOrganizationScopeResolver scopeResolver,
            IMediator mediator,
            [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var orgScope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, ct);
            var scopeId = scopeIdSelector(orgScope);

            var command = new CreateCustomBulkTemplateCommand(
                scope,
                scopeId,
                request.Name,
                request.Subject,
                request.TextBody,
                request.HtmlBody);

            var id = await mediator
                .SendReceiveAsync<CreateCustomBulkTemplateCommand, EmailTemplateId>(command, ct);

            await unitOfWork.SaveChangesAsync(ct);

            var location = eventSlug is not null
                ? $"/admin/teams/{teamSlug}/events/{eventSlug}/custom-bulk-templates/{id.Value}"
                : $"/admin/teams/{teamSlug}/custom-bulk-templates/{id.Value}";

            return TypedResults.Created(location, new CreateCustomBulkTemplateResponse(id.Value));
        }
    }
}

public sealed record CreateCustomBulkTemplateResponse(Guid Id);
