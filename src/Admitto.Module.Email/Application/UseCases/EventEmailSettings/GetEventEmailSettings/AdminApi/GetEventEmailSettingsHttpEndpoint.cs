using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings.AdminApi;

public static class GetEventEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapGetEventEmailSettings(this RouteGroupBuilder group)
    {
        group
            .MapGet("/", GetEventEmailSettings)
            .WithName(nameof(GetEventEmailSettings))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Ok<EventEmailSettingsDto>> GetEventEmailSettings(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);

        var dto = await mediator.QueryAsync<GetEventEmailSettingsQuery, EventEmailSettingsDto?>(
            new GetEventEmailSettingsQuery(scope.EventId!.Value),
            cancellationToken);

        if (dto is null)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.EventEmailSettings>(eventSlug));

        return TypedResults.Ok(dto);
    }
}
