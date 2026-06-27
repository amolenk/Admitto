namespace Amolenk.Admitto.Core.Registrations.Contracts;

public sealed record EventRegistrationSnapshotDto(
    string Name,
    string WebsiteUrl,
    string PublicEventLink,
    string RegisterLink,
    string QRCodeLink,
    string CancelLink,
    string TeamAccentColor,
    string? ChangeTicketsLink = null,
    string? FirstName = null,
    string? LastName = null)
{
    public EventRegistrationSnapshotDto(
        string name,
        string websiteUrl,
        string registerLink,
        string qrCodeLink,
        string cancelLink,
        string? firstName = null,
        string? lastName = null)
        : this(
            name,
            websiteUrl,
            websiteUrl,
            registerLink,
            qrCodeLink,
            cancelLink,
            "#2563eb",
            null,
            firstName,
            lastName)
    {
    }
}
