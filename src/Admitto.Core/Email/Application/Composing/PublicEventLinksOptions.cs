namespace Amolenk.Admitto.Core.Email.Application.Composing;

internal sealed class PublicEventLinksOptions
{
    // Public event links are owned and configured by Registrations.
    public const string SectionName = "Registrations:PublicEventLinks";
    public string BaseUrl { get; init; } = "http://localhost";
}
