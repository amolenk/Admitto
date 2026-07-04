using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

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

        TicketedEventRegistrationPolicy? policy = null;

        var hasAnyField = command.OpensAt is not null
            || command.ClosesAt is not null
            || command.AllowedEmailDomain is not null;

        if (hasAnyField)
        {
            if (command.OpensAt is null || command.ClosesAt is null)
            {
                throw new BusinessRuleViolationException(Errors.IncompletePolicy);
            }

            policy = TicketedEventRegistrationPolicy.Create(
                command.OpensAt.Value,
                command.ClosesAt.Value,
                command.AllowedEmailDomain);
        }

        ticketedEvent.ConfigureRegistrationPolicy(policy);
    }

    internal static class Errors
    {
        public static readonly Error IncompletePolicy = new(
            "configure_registration_policy.incomplete",
            "Registration policy requires OpensAt and ClosesAt when configuring a window — "
            + "send both (with an optional AllowedEmailDomain) to configure, or send no fields to clear.",
            Type: ErrorType.Validation);
    }
}
