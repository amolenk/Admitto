using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.UpdateEmailTemplate.AdminApi;

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
            .MapPut("/{id:guid}", UpdateEmailTemplate)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok> UpdateEmailTemplate(
        Guid id,
        UpdateEmailTemplateHttpRequest request,
        UpdateEmailTemplateHandler handler,
        [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        var command = new UpdateEmailTemplateCommand(
            id,
            request.Name,
            request.Subject,
            request.TextBody,
            request.HtmlBody,
            request.Version);

        await handler.HandleAsync(command, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return TypedResults.Ok();
    }
}
