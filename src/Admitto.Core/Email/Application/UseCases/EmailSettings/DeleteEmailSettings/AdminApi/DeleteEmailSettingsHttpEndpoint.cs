using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.DeleteEmailSettings.AdminApi;

public static class DeleteEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapDeleteEmailSettings(
        this RouteGroupBuilder group,
        bool isEventScoped)
    {
        var endpointName = isEventScoped ? "DeleteEventEmailSettings" : "DeleteTeamEmailSettings";
        var handler = new Handler(isEventScoped);

        group
            .MapDelete("/", handler.HandleAsync)
            .WithName(endpointName)
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private sealed class Handler(bool isEventScoped)
    {
        public async ValueTask<NoContent> HandleAsync(
            Guid teamId,
            Guid? eventId,
            [FromQuery] uint version,
            ICommandHandler<DeleteEmailSettingsCommand> handler,
            [FromKeyedServices(EmailModule.Key)] IUnitOfWork unitOfWork,
            CancellationToken ct)
        {
            var ticketedEventId = isEventScoped ? eventId!.Value : (Guid?)null;

            await handler.HandleAsync(new DeleteEmailSettingsCommand(teamId, ticketedEventId, version), ct);
            await unitOfWork.SaveChangesAsync(ct);

            return TypedResults.NoContent();
        }
    }
}
