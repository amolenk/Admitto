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

    private static async ValueTask<Ok<IReadOnlyList<RegistrationListItemDto>>> GetRegistrations(
        Guid teamId,
        Guid eventId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetRegistrationsQuery(TicketedEventId.From(eventId));

        var result = await mediator.QueryAsync<GetRegistrationsQuery, IReadOnlyList<RegistrationListItemDto>>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
