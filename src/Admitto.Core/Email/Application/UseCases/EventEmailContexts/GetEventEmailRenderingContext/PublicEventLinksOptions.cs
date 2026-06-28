namespace Amolenk.Admitto.Core.Email.Application.UseCases.EventEmailContexts.GetEventEmailRenderingContext;

/// <summary>
/// Deployment configuration for the public base URL used to derive per-event
/// public links (register/cancel/QR/change-tickets) from the stored public slug.
/// </summary>
public sealed class PublicEventLinksOptions
{
    public const string SectionName = "Registrations:PublicEventLinks";

    public string BaseUrl { get; init; } = "http://localhost";
}
