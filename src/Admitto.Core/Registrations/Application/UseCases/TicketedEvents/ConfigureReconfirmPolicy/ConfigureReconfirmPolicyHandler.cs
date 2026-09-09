using Amolenk.Admitto.Core.Registrations.Application.Persistence;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Application.Persistence;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ConfigureReconfirmPolicy;

internal sealed class ConfigureReconfirmPolicyHandler(IRegistrationsWriteStore writeStore)
    : ICommandHandler<ConfigureReconfirmPolicyCommand>
{
    public async ValueTask HandleAsync(
        ConfigureReconfirmPolicyCommand command,
        CancellationToken cancellationToken)
    {
        TicketedEventId eventId = TicketedEventId.From(command.EventId);
        TeamId teamId = TeamId.From(command.TeamId);

        var ticketedEvent = await writeStore.TicketedEvents.GetAsync(
            e => e.Id == eventId && e.TeamId == teamId,
            command.ExpectedVersion,
            cancellationToken);

        TicketedEventReconfirmPolicy? policy = null;

        var hasAnyField = command.OpensAt is not null
            || command.ClosesAt is not null
            || command.MinEmailIntervalHours is not null
            || command.QuietHoursStart is not null
            || command.QuietHoursEnd is not null;

        if (hasAnyField)
        {
            if (command.OpensAt is null || command.ClosesAt is null
                || command.MinEmailIntervalHours is null)
            {
                throw new BusinessRuleViolationException(Errors.IncompletePolicy);
            }

            policy = TicketedEventReconfirmPolicy.Create(
                command.OpensAt.Value,
                command.ClosesAt.Value,
                TimeSpan.FromHours(command.MinEmailIntervalHours.Value),
                command.QuietHoursStart,
                command.QuietHoursEnd);
        }

        ticketedEvent.ConfigureReconfirmPolicy(policy);
    }

    internal static class Errors
    {
        public static readonly Error IncompletePolicy = new(
            "configure_reconfirm_policy.incomplete",
            "Reconfirm policy requires OpensAt, ClosesAt, and MinEmailIntervalHours — send all required fields to configure or none to clear.",
            Type: ErrorType.Validation);
    }
}
