namespace Amolenk.Admitto.Core.Registrations.Application.ScannerLinks;

public sealed class ScannerLinksOptions
{
    public const string SectionName = "Registrations:ScannerLinks";

    public string BaseUrl { get; init; } = "http://localhost";
}
