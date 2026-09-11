using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

internal sealed class CheckInHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<CheckInCommand, CheckInResponse>
{
    public async ValueTask<CheckInResponse> HandleAsync(
        CheckInCommand command,
        CancellationToken cancellationToken)
    {
        var teamId = TeamId.From(command.TeamId);
        var eventId = TicketedEventId.From(command.EventId);
        var ticketedEvent = await writeStore.TicketedEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.TeamId == teamId, cancellationToken);

        if (ticketedEvent is null || !ticketedEvent.IsActive)
            return CheckInResponse.Invalid(CheckInOutcome.EventNotActive);

        if (!Guid.TryParse(command.Credential, out var credentialId))
            return CheckInResponse.Invalid(CheckInOutcome.InvalidForEvent);

        var registration = await writeStore.Registrations
            .FirstOrDefaultAsync(
            r => r.Id == RegistrationId.From(credentialId)
                && r.EventId == eventId
                && r.TeamId == teamId,
            cancellationToken);

        if (registration is null)
            return CheckInResponse.Invalid(CheckInOutcome.InvalidForEvent);

        if (registration.Status == RegistrationStatus.Cancelled)
            return CheckInResponse.ForRegistration(registration, CheckInOutcome.Cancelled);

        if (registration.CheckedInAt is not null)
            return CheckInResponse.ForRegistration(registration, CheckInOutcome.AlreadyCheckedIn);

        registration.CheckIn(timeProvider.GetUtcNow());
        return CheckInResponse.ForRegistration(registration, CheckInOutcome.Success);
    }
}
