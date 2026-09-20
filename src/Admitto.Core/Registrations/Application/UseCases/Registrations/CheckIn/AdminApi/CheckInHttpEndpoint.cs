using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn.AdminApi;

public static class CheckInHttpEndpoint
{
    public static RouteGroupBuilder MapCheckIn(this RouteGroupBuilder group)
    {
        group
            .MapPost("/check-in", CheckIn)
            .WithName(nameof(CheckIn))
            .RequireAuthorization(policy => policy.RequireTeamMembership(TeamMembershipRole.Crew));

        return group;
    }

    private static async ValueTask<Ok<CheckInResponse>> CheckIn(
        Guid teamId,
        Guid eventId,
        CheckInHttpRequest request,
        ICommandHandler<CheckInCommand, CheckInResponse> handler,
        [FromKeyedServices(RegistrationsModule.Key)] IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CheckInCommand(teamId, eventId, request.Credential),
            cancellationToken);

        result = await CheckInCommitter.CommitAsync(
            result, teamId, eventId, unitOfWork, scopeFactory, cancellationToken);

        return TypedResults.Ok(result);
    }
}
