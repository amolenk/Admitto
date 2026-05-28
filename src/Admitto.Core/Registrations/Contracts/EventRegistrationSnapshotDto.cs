namespace Amolenk.Admitto.Core.Registrations.Contracts;

public sealed record EventRegistrationSnapshotDto(
    string Name,
    string WebsiteUrl,
    string RegisterLink,
    string QRCodeLink,
    string? FirstName = null,
    string? LastName = null);
