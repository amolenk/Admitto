using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CancellationPolicy.GetCancellationPolicy.AdminApi;

public static class GetCancellationPolicyHttpEndpoint
{
    public static RouteGroupBuilder MapGetCancellationPolicy(this RouteGroupBuilder group)
    {
        group
            .MapGet("/cancellation-policy", GetCancellationPolicy)
            .WithName(nameof(GetCancellationPolicy))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok<CancellationPolicyDto>, NotFound>> GetCancellationPolicy(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var query = new GetCancellationPolicyQuery(TicketedEventId.From(scope.EventId!.Value));

        var result = await mediator.QueryAsync<GetCancellationPolicyQuery, CancellationPolicyDto?>(
            query, cancellationToken);

        if (result is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(result);
    }
}
