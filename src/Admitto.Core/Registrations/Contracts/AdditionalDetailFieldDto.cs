namespace Amolenk.Admitto.Core.Registrations.Contracts;

/// <summary>
/// A single field from the event's additional detail schema.
/// Returned by <see cref="IRegistrationsFacade.GetAdditionalDetailSchemaAsync"/>.
/// </summary>
public sealed record AdditionalDetailFieldDto(string Key, string Name);
