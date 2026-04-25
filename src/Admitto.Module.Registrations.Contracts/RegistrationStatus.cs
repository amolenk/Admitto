namespace Amolenk.Admitto.Module.Registrations.Contracts;

/// <summary>
/// Cross-module-visible registration lifecycle status. Mirrors the internal
/// Registrations status enum, exposed in <c>Contracts</c> so other modules
/// (e.g. Email, for bulk-email recipient resolution) can filter on it without
/// taking a dependency on the module-internal aggregate.
/// </summary>
public enum RegistrationStatus
{
    Registered = 0,
    Cancelled = 1
}
