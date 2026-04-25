namespace Amolenk.Admitto.Module.Email.Domain.ValueObjects;

/// <summary>
/// A single literal recipient supplied as part of an
/// <see cref="ExternalListSource"/>.
/// </summary>
public sealed record ExternalListItem(string Email, string? DisplayName);
