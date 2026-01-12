namespace Amolenk.Admitto.Domain.ValueObjects;

public record ReconfirmPolicy(
    TimeSpan WindowStartBeforeEvent,
    TimeSpan WindowEndBeforeEvent,
    TimeSpan InitialDelayAfterRegistration,
    TimeSpan ReminderInterval);