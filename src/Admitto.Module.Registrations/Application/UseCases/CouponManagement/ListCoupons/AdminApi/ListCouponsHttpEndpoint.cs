using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.CouponManagement.ListCoupons.AdminApi;

public static class ListCouponsHttpEndpoint
{
    public static RouteGroupBuilder MapListCoupons(this RouteGroupBuilder group)
    {
        group
            .MapGet("/coupons", ListCoupons)
            .WithName(nameof(ListCoupons))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<ListCouponsResult>> ListCoupons(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var query = new ListCouponsQuery(TicketedEventId.From(scope.EventId!.Value));

        var result = await mediator.QueryAsync<ListCouponsQuery, ListCouponsResult>(
            query, cancellationToken);

        return TypedResults.Ok(result);
    }
}
