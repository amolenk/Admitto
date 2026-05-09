namespace Amolenk.Admitto.Core.Email.Domain.ValueObjects;

/// <summary>
/// A single literal recipient supplied as part of an
/// <see cref="ExternalListSource"/>.
/// </summary>
public sealed record ExternalListItem(string Email, string? DisplayName);
