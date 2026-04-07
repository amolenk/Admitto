using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketTypeManagement.GetTicketTypes.AdminApi;

public static class GetTicketTypesHttpEndpoint
{
    public static RouteGroupBuilder MapGetTicketTypes(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetTicketTypes)
            .WithName(nameof(GetTicketTypes))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<IReadOnlyList<TicketTypeDto>>> GetTicketTypes(
        OrganizationScope organizationScope,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetTicketTypesQuery(TicketedEventId.From(organizationScope.EventId!.Value));

        var result = await mediator.QueryAsync<GetTicketTypesQuery, IReadOnlyList<TicketTypeDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
