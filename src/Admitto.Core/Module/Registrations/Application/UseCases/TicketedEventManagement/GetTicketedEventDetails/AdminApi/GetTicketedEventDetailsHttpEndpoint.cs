using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.GetTicketedEventDetails.AdminApi;

public static class GetTicketedEventDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketedEventDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketedEventDetails)
            .WithName(nameof(GetTicketedEventDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Results<Ok<TicketedEventDetailsDto>, NotFound>> GetTicketedEventDetails(
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketedEventDetailsQuery(TicketedEventId.From(eventId));

        var result = await mediator.QueryAsync<GetTicketedEventDetailsQuery, TicketedEventDetailsDto?>(
            query, cancellationToken);

        return result is null ? TypedResults.NotFound() : TypedResults.Ok(result);
    }
}
