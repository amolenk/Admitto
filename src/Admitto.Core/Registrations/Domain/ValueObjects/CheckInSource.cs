namespace Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;

/// <summary>
/// Distinguishes the access context that performed a check-in, for activity-log
/// context only. It never infers an individual door-assistant identity.
/// </summary>
public enum CheckInSource
{
    Dashboard,
    SharedScanner
}
