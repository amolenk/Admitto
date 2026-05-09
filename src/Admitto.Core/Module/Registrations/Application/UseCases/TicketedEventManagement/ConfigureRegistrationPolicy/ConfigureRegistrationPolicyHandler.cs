using Amolenk.Admitto.Core.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureRegistrationPolicy;

internal sealed class ConfigureRegistrationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureRegistrationPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureRegistrationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        var policy = TicketedEventRegistrationPolicy.Create(
            command.OpensAt,
            command.ClosesAt,
            command.AllowedEmailDomain);

        ticketedEvent.ConfigureRegistrationPolicy(policy);
    }
}
