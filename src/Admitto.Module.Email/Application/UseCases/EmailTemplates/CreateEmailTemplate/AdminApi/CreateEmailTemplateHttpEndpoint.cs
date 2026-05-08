using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate.AdminApi;

public static class CreateEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapCreateEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "CreateTeamEmailTemplate"
            : "CreateEventEmailTemplate";

        var handler = new Handler(scope);

        group
            .MapPost("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope)
    {
        public async ValueTask<Created<CreateEmailTemplateResponse>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            CreateEmailTemplateHttpRequest request,
            IMediator mediator,
            [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;
            var parentScopeId = scope == EmailSettingsScope.Event ? teamId : (Guid?)null;

            var command = new CreateEmailTemplateCommand(
                scope,
                scopeId,
                request.Name,
                request.Subject,
                request.TextBody,
                request.HtmlBody,
                parentScopeId);

            var id = await mediator
                .SendReceiveAsync<CreateEmailTemplateCommand, Guid>(command, ct);

            await unitOfWork.SaveChangesAsync(ct);

            var location = eventId is not null
                ? $"/admin/teams/{teamId}/events/{eventId}/email-templates/{id}"
                : $"/admin/teams/{teamId}/email-templates/{id}";

            return TypedResults.Created(location, new CreateEmailTemplateResponse(id));
        }
    }
}

public sealed record CreateEmailTemplateResponse(Guid Id);
