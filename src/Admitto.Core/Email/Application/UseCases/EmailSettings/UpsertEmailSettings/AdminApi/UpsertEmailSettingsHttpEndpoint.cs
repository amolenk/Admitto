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
        this RouteGroupBuilder group,
        EmailSettingsScope scope)
    {
        var endpointName = scope == EmailSettingsScope.Team ? "UpsertTeamEmailSettings" : "UpsertEventEmailSettings";
        var handler = new Handler(scope);

        group
            .MapPut("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(EmailSettingsScope scope)
    {
        public async ValueTask<Results<Ok, Created>> HandleAsync(
            Guid teamId,
            Guid? eventId,
            UpsertEmailSettingsHttpRequest request,
            ICommandHandler<CreateEmailSettingsCommand> createHandler,
            ICommandHandler<UpdateEmailSettingsCommand> updateHandler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var scopeId = EmailScopeId.From(scope == EmailSettingsScope.Event ? eventId!.Value : teamId);

            if (request.Version is { } expectedVersion)
            {
                await updateHandler.HandleAsync(request.ToUpdateCommand(scope, scopeId, expectedVersion), ct);
                await unitOfWork.SaveChangesAsync(ct);
                return TypedResults.Ok();
            }

            await createHandler.HandleAsync(request.ToCreateCommand(scope, scopeId), ct);
            await unitOfWork.SaveChangesAsync(ct);

            var location = eventId is not null
                ? $"/admin/teams/{teamId}/events/{eventId}/email-settings"
                : $"/admin/teams/{teamId}/email-settings";

            return TypedResults.Created(location);
        }
    }
}
