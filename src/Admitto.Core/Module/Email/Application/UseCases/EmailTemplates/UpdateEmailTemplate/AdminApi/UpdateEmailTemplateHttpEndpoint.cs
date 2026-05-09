using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

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
