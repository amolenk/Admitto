namespace Amolenk.Admitto.Application.UseCases.TicketedEvents.SetReconfirmPolicy;

public record SetReconfirmPolicyRequest(
    TimeSpan WindowStartBeforeEvent,
    TimeSpan WindowEndBeforeEvent,
    TimeSpan InitialDelayAfterRegistration,
    TimeSpan ReminderInterval);
