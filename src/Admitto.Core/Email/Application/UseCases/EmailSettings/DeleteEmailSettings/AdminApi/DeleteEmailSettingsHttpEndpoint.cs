using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Http;
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
        var handler = new Handler(scope);

        group
            .MapDelete("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope)
    {
        public async ValueTask<NoContent> HandleAsync(
            Guid teamId,
            Guid? eventId,
            [FromQuery] uint version,
            DeleteEmailSettingsHandler handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var scopeId = EmailScopeId.From(scope == EmailSettingsScope.Event ? eventId!.Value : teamId);

            await handler.HandleAsync(new DeleteEmailSettingsCommand(scope, scopeId, version), ct);
            await unitOfWork.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}
