using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.DeleteCustomBulkTemplate.AdminApi;

public static class DeleteCustomBulkTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteCustomBulkTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team
            ? "DeleteTeamCustomBulkTemplate"
            : "DeleteEventCustomBulkTemplate";

        group
            .MapDelete("/{id:guid}", async (
                string teamSlug,
                string? eventSlug,
                Guid id,
                IMediator mediator,
                [FromKeyedServices(EmailModuleKey.Value)] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                await mediator.SendAsync(
                    new DeleteCustomBulkTemplateCommand(EmailTemplateId.From(id)), ct);

                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
