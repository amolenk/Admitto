using Amolenk.Admitto.Module.Registrations.Application.Persistence;
using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Application.Persistence;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.TicketedEventManagement.ConfigureReconfirmPolicy;

internal sealed class ConfigureReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureReconfirmPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureReconfirmPolicyCommand command,
        CancellationToken cancellationToken)
    {
        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            command.EventId,
            command.ExpectedVersion,
            cancellationToken);

        TicketedEventReconfirmPolicy? policy = null;

        var hasAnyField = command.OpensAt is not null
            || command.ClosesAt is not null
            || command.CadenceDays is not null;

        if (hasAnyField)
        {
            if (command.OpensAt is null || command.ClosesAt is null || command.CadenceDays is null)
            {
                throw new BusinessRuleViolationException(Errors.IncompletePolicy);
            }

            policy = TicketedEventReconfirmPolicy.Create(
                command.OpensAt.Value,
                command.ClosesAt.Value,
                TimeSpan.FromDays(command.CadenceDays.Value));
        }

        ticketedEvent.ConfigureReconfirmPolicy(policy);
    }

    internal static class Errors
    {
        public static readonly Error IncompletePolicy = new(
            "configure_reconfirm_policy.incomplete",
            "Reconfirm policy requires OpensAt, ClosesAt, and CadenceDays — send all three to configure or none to clear.",
            Type: ErrorType.Validation);
    }
}
