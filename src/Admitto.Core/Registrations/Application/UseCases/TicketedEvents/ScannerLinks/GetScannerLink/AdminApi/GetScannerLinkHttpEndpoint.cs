using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ScannerLinks.GetScannerLink.AdminApi;

public static class GetScannerLinkHttpEndpoint
{
    public static RouteGroupBuilder MapGetScannerLink(this RouteGroupBuilder group)
    {
        group
            .MapGet("/scanner-link", GetScannerLink)
            .WithName(nameof(GetScannerLink))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<ScannerLinkDto>, NotFound>> GetScannerLink(
        Guid teamId,
        Guid eventId,
        IQueryHandler<GetScannerLinkQuery, ScannerLinkDto?> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetScannerLinkQuery(teamId, eventId), cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
