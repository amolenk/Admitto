using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureRegistrationPolicy;

internal sealed class ConfigureRegistrationPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureRegistrationPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureRegistrationPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        var policy = TicketedEventRegistrationPolicy.Create(
            command.OpensAt,
            command.ClosesAt,
            command.AllowedEmailDomain);

        ticketedEvent.ConfigureRegistrationPolicy(policy);
    }
}
