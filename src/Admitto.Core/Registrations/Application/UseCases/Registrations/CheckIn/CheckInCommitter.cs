using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

/// <summary>
/// Commits a successful <see cref="CheckInCommand"/> result and reconciles the rare
/// concurrent-scan race: if another request already checked in (or cancelled) the same
/// registration between the handler's read and this commit, the optimistic-concurrency
/// conflict is resolved to the registration's persisted outcome instead of surfacing a
/// public conflict error. Shared by every check-in endpoint (admin and shared-scanner)
/// so the reconciliation behavior stays identical across access contexts.
/// </summary>
internal static class CheckInCommitter
{
    public static async ValueTask<CheckInResponse> CommitAsync(
        CheckInResponse result,
        Guid teamId,
        Guid eventId,
        IUnitOfWork unitOfWork,
        IServiceScopeFactory scopeFactory,
        CancellationToken cancellationToken)
    {
        if (result.Outcome != CheckInOutcome.Success)
            return result;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken, retryConcurrencyConflicts: true);
            return result;
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
                return CheckInResponse.ForRegistration(registration, CheckInOutcome.AlreadyCheckedIn);

            if (registration?.Status == RegistrationStatus.Cancelled)
                return CheckInResponse.ForRegistration(registration, CheckInOutcome.Cancelled);

            throw new BusinessRuleViolationException(ConcurrencyConflictError.Create());
        }
    }
}
