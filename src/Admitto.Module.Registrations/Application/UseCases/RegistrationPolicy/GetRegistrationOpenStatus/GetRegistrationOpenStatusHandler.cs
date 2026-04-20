using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Application.Services;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.RegistrationPolicy.GetRegistrationOpenStatus;

internal sealed class GetRegistrationOpenStatusHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : IQueryHandler<GetRegistrationOpenStatusQuery, RegistrationOpenStatusDto>
{
    public async ValueTask<RegistrationOpenStatusDto> HandleAsync(
        GetRegistrationOpenStatusQuery query,
        CancellationToken cancellationToken)
    {
        var guard = await writeStore.TicketedEventLifecycleGuards
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == query.EventId, cancellationToken);

        var isEventActive = guard?.IsActive ?? true;

        var policy = await writeStore.EventRegistrationPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == query.EventId, cancellationToken);

        var isOpen = isEventActive && (policy?.IsRegistrationOpen(timeProvider.GetUtcNow()) ?? false);

        return new RegistrationOpenStatusDto(
            isOpen,
            isEventActive,
            policy?.RegistrationWindowOpensAt,
            policy?.RegistrationWindowClosesAt);
    }
}
