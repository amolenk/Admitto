using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.Emails.ComposeTransactionalEmail;

internal sealed record ReconfirmationIntent(string FirstName, RegistrationId RegistrationId);

internal sealed record ReconfirmationEmailCompositionScope(
    TransactionalEmailContext Context,
    EmailTemplate Template,
    IEmailRenderer Renderer)
{
    public RenderedEmail Render(ReconfirmationIntent intent)
    {
        var links = Context.GetLinks(intent.RegistrationId);
        var parameters = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = intent.FirstName,
            ["event_name"] = Context.EventName,
            ["reconfirm_link"] = links.ReconfirmLink,
            ["cancel_link"] = links.CancelLink,
            ["event_website"] = Context.WebsiteUrl,
            ["accent_color"] = Context.AccentColor.Value,
            ["font_family"] = EmailFontFamily.Default
        };
        return Renderer.Render(Template, parameters);
    }
}
