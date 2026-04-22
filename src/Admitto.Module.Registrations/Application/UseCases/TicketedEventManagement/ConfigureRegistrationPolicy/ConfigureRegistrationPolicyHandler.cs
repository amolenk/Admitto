using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

internal sealed class ConfigureRegistrationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureRegistrationPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureRegistrationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        var policy = TicketedEventRegistrationPolicy.Create(
            command.OpensAt,
            command.ClosesAt,
            command.AllowedEmailDomain);

        ticketedEvent.ConfigureRegistrationPolicy(policy);
    }
}
