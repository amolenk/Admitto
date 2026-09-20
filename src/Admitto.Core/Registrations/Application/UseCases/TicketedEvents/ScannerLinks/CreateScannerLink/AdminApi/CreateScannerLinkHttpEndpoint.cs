using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.CreateScannerLink.AdminApi;

public static class CreateScannerLinkHttpEndpoint
{
    public static RouteGroupBuilder MapCreateScannerLink(this RouteGroupBuilder group)
    {
        group
            .MapPost("/scanner-link", CreateScannerLink)
            .WithName(nameof(CreateScannerLink))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<ScannerLinkDto>> CreateScannerLink(
        Guid teamId,
        Guid eventId,
        ICommandHandler<CreateScannerLinkCommand, ScannerLinkDto> handler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateScannerLinkCommand(teamId, eventId), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(result);
    }
}
