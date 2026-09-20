using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.RevokeScannerLink.AdminApi;

public static class RevokeScannerLinkHttpEndpoint
{
    public static RouteGroupBuilder MapRevokeScannerLink(this RouteGroupBuilder group)
    {
        group
            .MapPost("/scanner-link/revoke", RevokeScannerLink)
            .WithName(nameof(RevokeScannerLink))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<ScannerLinkDto>> RevokeScannerLink(
        Guid teamId,
        Guid eventId,
        ICommandHandler<RevokeScannerLinkCommand, ScannerLinkDto> handler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new RevokeScannerLinkCommand(teamId, eventId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(result);
    }
}
