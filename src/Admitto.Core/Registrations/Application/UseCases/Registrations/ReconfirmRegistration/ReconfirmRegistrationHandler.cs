using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.ReconfirmRegistration;

internal sealed class ReconfirmRegistrationHandler(
    IRegistrationsWriteStore writeStore,
    TimeProvider timeProvider)
    : ICommandHandler<ReconfirmRegistrationCommand>
{
    public async ValueTask HandleAsync(
        ReconfirmRegistrationCommand command,
        CancellationToken cancellationToken)
    {
        var registrationId = RegistrationId.From(command.RegistrationId);
        var ticketedEventId = TicketedEventId.From(command.TicketedEventId);
        var teamId = TeamId.From(command.TeamId);

        var registration = await writeStore.Registrations.GetAsync(
            r => r.Id == registrationId && r.EventId == ticketedEventId && r.TeamId == teamId,
            cancellationToken);

        registration.Reconfirm(timeProvider.GetUtcNow());
    }
}
