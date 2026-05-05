using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.UpdateCustomBulkTemplate.AdminApi;

public static class UpdateCustomBulkTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapUpdateCustomBulkTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "UpdateTeamCustomBulkTemplate"
            : "UpdateEventCustomBulkTemplate";

        group
            .MapPut("/{id:guid}", async (
                string teamSlug,
                string? eventSlug,
                Guid id,
                UpdateCustomBulkTemplateHttpRequest request,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                var command = new UpdateCustomBulkTemplateCommand(
                    EmailTemplateId.From(id),
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
