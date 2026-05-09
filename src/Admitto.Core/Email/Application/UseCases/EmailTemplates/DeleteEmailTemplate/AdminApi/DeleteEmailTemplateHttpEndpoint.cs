using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate.AdminApi;

public static class DeleteEmailTemplateHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailTemplate(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "DeleteTeamEmailTemplate" : "DeleteEventEmailTemplate";

        group
            .MapDelete("/{id:guid}", async (
                Guid id,
                [FromQuery] uint version,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var unitOfWork = httpContext.RequestServices
                    .GetRequiredKeyedService<IUnitOfWork>(EmailModuleKey.Value);

                await mediator.SendAsync(new DeleteEmailTemplateCommand(id, version), ct);
                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
