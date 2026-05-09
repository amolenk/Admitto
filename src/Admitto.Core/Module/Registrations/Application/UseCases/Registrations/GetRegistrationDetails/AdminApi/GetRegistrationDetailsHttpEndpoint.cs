using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.GetRegistrationDetails.AdminApi;

public static class GetRegistrationDetailsHttpEndpoint
{
    public static RouteGroupBuilder MapGetRegistrationDetails(this RouteGroupBuilder group)
    {
        group
            .MapGet("/registrations/{registrationId:guid}", GetRegistrationDetails)
            .WithName(nameof(GetRegistrationDetails))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<RegistrationDetailDto>, NotFound>> GetRegistrationDetails(
        Guid teamId,
        Guid eventId,
        Guid registrationId,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var query = new GetRegistrationDetailsQuery(
            TeamId: teamId,
            EventId: TicketedEventId.From(eventId),
            RegistrationId: RegistrationId.From(registrationId));

        var result = await mediator.QueryAsync<GetRegistrationDetailsQuery, RegistrationDetailDto?>(
            query, cancellationToken);

        if (result is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(result);
    }
}
