namespace Amolenk.Admitto.Module.Organization.Domain.ValueObjects;

/// <summary>
/// Lifecycle of a <see cref="Entities.TeamEventCreationRequest"/>.
/// </summary>
public enum TeamEventCreationRequestStatus
{
    Pending = 0,
    Created = 1,
    Rejected = 2,
    Expired = 3
}
