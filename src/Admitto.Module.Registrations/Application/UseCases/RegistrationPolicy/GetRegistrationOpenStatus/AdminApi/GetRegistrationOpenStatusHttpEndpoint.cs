using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus.AdminApi;

public static class GetRegistrationOpenStatusHttpEndpoint
{
    public static RouteGroupBuilder MapGetRegistrationOpenStatus(this RouteGroupBuilder group)
    {
        group
            .MapGet("/registration/open-status", GetRegistrationOpenStatus)
            .WithName(nameof(GetRegistrationOpenStatus))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<RegistrationOpenStatusDto>> GetRegistrationOpenStatus(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var query = new GetRegistrationOpenStatusQuery(TicketedEventId.From(scope.EventId!.Value));

        var result = await mediator.QueryAsync<GetRegistrationOpenStatusQuery, RegistrationOpenStatusDto>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
