namespace Amolenk.Admitto.Core.Registrations.Application.PublicEventLinks;

public sealed class PublicEventLinksOptions
{
    public const string SectionName = "Registrations:PublicEventLinks";

    public string BaseUrl { get; init; } = "http://localhost";
}
