using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RegenerateScannerLink.AdminApi;

public static class RegenerateScannerLinkHttpEndpoint
{
    public static RouteGroupBuilder MapRegenerateScannerLink(this RouteGroupBuilder group)
    {
        group
            .MapPost("/scanner-link/regenerate", RegenerateScannerLink)
            .WithName(nameof(RegenerateScannerLink))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<ScannerLinkDto>> RegenerateScannerLink(
        Guid teamId,
        Guid eventId,
        ICommandHandler<RegenerateScannerLinkCommand, ScannerLinkDto> handler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RegenerateScannerLinkCommand(teamId, eventId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(result);
    }
}
