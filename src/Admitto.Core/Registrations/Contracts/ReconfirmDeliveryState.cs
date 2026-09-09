namespace Amolenk.Admitto.Core.Registrations.Contracts;

public enum ReconfirmDeliverySuppression
{
    EventNotActive,
    PolicyDisabled,
    OutsideWindow,
    QuietHours,
    InvalidTimeZone,
    RegistrationNotFound,
    RegistrationCancelled,
    RegistrationReconfirmed,
    RegistrationCycleChanged,
    TicketSelectionChanged,
}

/// <summary>
/// Live Registrations-owned result used by Email immediately before a queued
/// reconfirmation is delivered. The result shape makes an allowed response
/// carry every value required by Email; suppressed responses carry no partial
/// eligibility data.
/// </summary>
public abstract record ReconfirmDeliveryState
{
    public sealed record Allowed(
        DateTimeOffset RegistrationCreatedAt,
        TimeSpan MinimumEmailInterval,
        int? EffectiveMaxReconfirmationEmails,
        DateTimeOffset DeliveryCutoffAt) : ReconfirmDeliveryState;

    public sealed record Suppressed(ReconfirmDeliverySuppression Reason) : ReconfirmDeliveryState;
}
