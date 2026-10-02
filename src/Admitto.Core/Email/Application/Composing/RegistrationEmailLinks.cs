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
        RegistrationId? registrationId,
        string? couponCode = null)
    {
        var registrationSuffix = registrationId?.Value.ToString();
        var hasRegistration = !string.IsNullOrWhiteSpace(registrationSuffix);

        var registerLink = $"{publicEventLink}/register";
        var editLink = hasRegistration ? $"{publicEventLink}/edit/{registrationSuffix}" : publicEventLink;

        return new RegistrationEmailLinks(
            publicEventLink,
            AppendCouponCode(registerLink, couponCode),
            hasRegistration ? $"{publicEventLink}/qr-code/{registrationSuffix}" : publicEventLink,
            hasRegistration ? $"{publicEventLink}/cancel/{registrationSuffix}" : publicEventLink,
            AppendCouponCode(editLink, couponCode),
            hasRegistration ? $"{publicEventLink}/reconfirm/{registrationSuffix}" : publicEventLink);
    }

    private static string AppendCouponCode(string link, string? couponCode) =>
        string.IsNullOrWhiteSpace(couponCode)
            ? link
            : $"{link}?coupon={Uri.EscapeDataString(couponCode)}";
}
