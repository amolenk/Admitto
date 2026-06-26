using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;

public static class DeleteEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailSettings(
        this RouteGroupBuilder group)
    {
        group
            .MapDelete("/", HandleAsync)
            .WithName("DeleteTeamEmailSettings")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<NoContent> HandleAsync(
        Guid teamId,
        [FromQuery] uint version,
        ICommandHandler<DeleteEmailSettingsCommand> handler,
        [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        await handler.HandleAsync(new DeleteEmailSettingsCommand(teamId, version), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
