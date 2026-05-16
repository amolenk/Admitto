using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed class ConfigureReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureReconfirmPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureReconfirmPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            eventId,
            command.ExpectedVersion,
            cancellationToken);

        TicketedEventReconfirmPolicy? policy = null;

        var hasAnyField = command.OpensAt is not null
            || command.ClosesAt is not null
            || command.CadenceHours is not null
            || command.MinEmailIntervalHours is not null;

        if (hasAnyField)
        {
            if (command.OpensAt is null || command.ClosesAt is null
                || command.CadenceHours is null || command.MinEmailIntervalHours is null)
            {
                throw new BusinessRuleViolationException(Errors.IncompletePolicy);
            }

            policy = TicketedEventReconfirmPolicy.Create(
                command.OpensAt.Value,
                command.ClosesAt.Value,
                TimeSpan.FromHours(command.CadenceHours.Value),
                TimeSpan.FromHours(command.MinEmailIntervalHours.Value));
        }

        ticketedEvent.ConfigureReconfirmPolicy(policy);
    }

    internal static class Errors
    {
        public static readonly Error IncompletePolicy = new(
            "configure_reconfirm_policy.incomplete",
            "Reconfirm policy requires OpensAt, ClosesAt, CadenceHours, and MinEmailIntervalHours — send all four to configure or none to clear.",
            Type: ErrorType.Validation);
    }
}
