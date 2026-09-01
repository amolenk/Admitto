using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Application.Templating.EventEmailRenderingContext;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeReconfirmation;

/// <summary>
/// Registration-specific facts used to compose one reconfirmation message.
/// Event facts, links, template, and branding belong to the composition scope.
/// </summary>
internal sealed record ReconfirmationIntent(
    string FirstName,
    RegistrationId RegistrationId);

/// <summary>
/// Fully initialized immutable scope for an event's reconfirmation messages.
/// Event context, effective template, branding, and renderer are loaded once.
/// </summary>
internal sealed record ReconfirmationEmailCompositionScope(
    EventEmailRenderingScope EventContext,
    EmailTemplate Template,
    EmailFontFamily FontFamily,
    IEmailRenderer Renderer)
{
    public RegistrationEmailLinks GetRegistrationLinks(RegistrationId registrationId) =>
        EventContext.GetRegistrationLinks(registrationId);

    public RenderedEmail Render(ReconfirmationIntent intent)
    {
        var links = GetRegistrationLinks(intent.RegistrationId);
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = intent.FirstName,
            ["event_name"] = EventContext.EventName,
            ["reconfirm_link"] = links.ReconfirmLink,
            ["cancel_link"] = links.CancelLink,
            ["event_website"] = EventContext.WebsiteUrl
        };

        return Renderer.Render(
            Template,
            EmailTemplateParameters.WithBranding(
                parameters,
                EventContext.AccentColor,
                FontFamily));
    }
}
