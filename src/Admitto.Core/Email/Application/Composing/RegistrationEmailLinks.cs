using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.Composing;

internal sealed record RegistrationEmailLinks(
    string PublicEventLink,
    string RegisterLink,
    string QRCodeLink,
    string CancelLink,
    string EditRegistrationLink,
    string ReconfirmLink)
{
    public static RegistrationEmailLinks From(
        string publicEventLink,
        RegistrationId? registrationId)
    {
        var registrationSuffix = registrationId?.Value.ToString();
        var hasRegistration = !string.IsNullOrWhiteSpace(registrationSuffix);

        return new RegistrationEmailLinks(
            publicEventLink,
            $"{publicEventLink}/register",
            hasRegistration ? $"{publicEventLink}/qr-code/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/cancel/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/edit/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/reconfirm/{registrationSuffix}" : publicEventLink);
    }
}
