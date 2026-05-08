using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

public static class UpdateEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "UpdateTeamEmailTemplate"
            : "UpdateEventEmailTemplate";

        group
            .MapPut("/{id:guid}", async (
                Guid id,
                UpdateEmailTemplateHttpRequest request,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                var command = new UpdateEmailTemplateCommand(
                    id,
                    request.Name,
                    request.Subject,
                    request.TextBody,
                    request.HtmlBody,
                    request.Version);

                await mediator.SendAsync(command, ct);
                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.Ok();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
