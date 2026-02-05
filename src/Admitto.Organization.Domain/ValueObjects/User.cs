using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Domain.ValueObjects;

/// <summary>
/// Represents a user of the system.
/// </summary>
public record User(UserId Id, EmailAddress Email);