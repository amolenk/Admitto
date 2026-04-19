namespace Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;

/// <summary>
/// Explicit registration status for an event. Tracks whether organizers have opened the
/// event for registration (in addition to the existing window/lifecycle checks).
/// </summary>
public enum RegistrationStatus
{
    Draft = 0,
    Open = 1,
    Closed = 2
}
