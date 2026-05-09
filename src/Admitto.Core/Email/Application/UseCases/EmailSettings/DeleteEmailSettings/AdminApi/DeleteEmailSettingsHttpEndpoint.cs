using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;

public static class DeleteEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailSettings(
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "DeleteTeamEmailSettings" : "DeleteEventEmailSettings";

        group
            .MapDelete("/", async (
                Guid teamId,
                Guid? eventId,
                [FromQuery] uint version,
                IMediator mediator,
                HttpContext httpContext,
                CancellationToken ct) =>
            {
                var unitOfWork = httpContext.RequestServices
                    .GetRequiredKeyedService<IUnitOfWork>(EmailModuleKey.Value);

                var scopeId = scope == EmailSettingsScope.Event ? eventId!.Value : teamId;

                await mediator.SendAsync(new DeleteEmailSettingsCommand(scope, scopeId, version), ct);
                await unitOfWork.SaveChangesAsync(ct);

                return TypedResults.NoContent();
            })
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }
}
