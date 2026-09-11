using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Auth;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

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

        if (result.Outcome == CheckInOutcome.Success)
        {
            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken, retryConcurrencyConflicts: true);
            }
            catch (DbUpdateConcurrencyException)
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var freshStore = scope.ServiceProvider.GetRequiredService<IRegistrationsWriteStore>();
                var registration = await freshStore.Registrations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        r => r.Id == RegistrationId.From(result.RegistrationId!.Value)
                             && r.TeamId == TeamId.From(teamId)
                             && r.EventId == TicketedEventId.From(eventId),
                        cancellationToken);

                if (registration?.CheckedInAt is not null)
                    result = CheckInResponse.ForRegistration(registration, CheckInOutcome.AlreadyCheckedIn);
                else if (registration?.Status == RegistrationStatus.Cancelled)
                    result = CheckInResponse.ForRegistration(registration, CheckInOutcome.Cancelled);
                else
                    throw new BusinessRuleViolationException(ConcurrencyConflictError.Create());
            }
        }

        return TypedResults.Ok(result);
    }
}
