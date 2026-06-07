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
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "UpsertEventEmailSettings" : "UpsertTeamEmailSettings";
        var handler = new Handler(isEventScoped);

        group
            .MapPut("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
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
            var ticketedEventId = isEventScoped ? eventId!.Value : (Guid?)null;

            if (request.Version is { } expectedVersion)
            {
                await updateHandler.HandleAsync(request.ToUpdateCommand(teamId, ticketedEventId, expectedVersion), ct);
                await unitOfWork.SaveChangesAsync(ct);
                return TypedResults.Ok();
            }

            await createHandler.HandleAsync(request.ToCreateCommand(teamId, ticketedEventId), ct);
            await unitOfWork.SaveChangesAsync(ct);

            var location = eventId is not null
                ? $"/admin/teams/{teamId}/events/{eventId}/email-settings"
                : $"/admin/teams/{teamId}/email-settings";

            return TypedResults.Created(location);
        }
    }
}
