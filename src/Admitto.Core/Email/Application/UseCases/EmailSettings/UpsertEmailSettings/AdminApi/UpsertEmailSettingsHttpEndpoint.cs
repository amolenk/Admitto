using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.CreateEmailSettings;
using Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpdateEmailSettings;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.UpsertEmailSettings.AdminApi;

public static class UpsertEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapUpsertEmailSettings(
        this RouteGroupBuilder group)
    {
        group
            .MapPut("/", HandleAsync)
            .WithName("UpsertTeamEmailSettings")
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok, Created>> HandleAsync(
        Guid teamId,
        UpsertEmailSettingsHttpRequest request,
        ICommandHandler<CreateEmailSettingsCommand> createHandler,
        ICommandHandler<UpdateEmailSettingsCommand> updateHandler,
        [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken ct)
    {
        if (request.Version is { } expectedVersion)
        {
            await updateHandler.HandleAsync(request.ToUpdateCommand(teamId, expectedVersion), ct);
            await unitOfWork.SaveChangesAsync(ct);
            return TypedResults.Ok();
        }

        await createHandler.HandleAsync(request.ToCreateCommand(teamId), ct);
        await unitOfWork.SaveChangesAsync(ct);

        return TypedResults.Created($"/admin/teams/{teamId}/email-settings");
    }
}
