using Amolenk.Admitto.Module.Email.Application;
using Amolenk.Admitto.Module.Shared.Application.Auth;
using Amolenk.Admitto.Module.Shared.Application.Http;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.UpsertEventEmailSettings.AdminApi;

public static class UpsertEventEmailSettingsHttpEndpoint
{
    public static RouteGroupBuilder MapUpsertEventEmailSettings(this RouteGroupBuilder group)
    {
        group
            .MapPut("/", UpsertEventEmailSettings)
            .WithName(nameof(UpsertEventEmailSettings))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Organizer));

        return group;
    }

    private static async ValueTask<Results<Ok, Created>> UpsertEventEmailSettings(
        string teamSlug,
        string eventSlug,
        IOrganizationScopeResolver scopeResolver,
        UpsertEventEmailSettingsHttpRequest request,
        IMediator mediator,
        [FromKeyedServices(EmailModuleKey.Value)]
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var scope = await scopeResolver.ResolveAsync(teamSlug, eventSlug, cancellationToken);
        var ticketedEventId = scope.EventId!.Value;

        if (request.Version is { } expectedVersion)
        {
            await mediator.SendAsync(request.ToUpdateCommand(ticketedEventId, expectedVersion), cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return TypedResults.Ok();
        }

        await mediator.SendAsync(request.ToCreateCommand(ticketedEventId), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return TypedResults.Created($"/admin/teams/{teamSlug}/events/{eventSlug}/email-settings");
    }
}
