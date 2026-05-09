using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations.AdminApi;

public static class GetRegistrationsHttpEndpoint
{
    public static RouteGroupBuilder MapGetRegistrations(this RouteGroupBuilder group)
    {
        group
            .MapGet("/registrations", GetRegistrations)
            .WithName(nameof(GetRegistrations))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<IReadOnlyList<RegistrationListItemDto>>, NotFound>> GetRegistrations(
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetRegistrationsQuery(TeamId.From(teamId), TicketedEventId.From(eventId));

        var result = await mediator.QueryAsync<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>?>(
            query, cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(result);
    }
}
