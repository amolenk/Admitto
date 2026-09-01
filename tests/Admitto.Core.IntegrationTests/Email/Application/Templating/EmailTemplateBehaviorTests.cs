using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Templating;

[TestClass]
public sealed class EmailTemplateBehaviorTests
{
    // Given ticket confirmation template parameters with an event website and public event link
    // When the ticket confirmation template is rendered
    // Then links use the public event link and show edit-registration wording
    [TestMethod]
    public void TicketConfirmationTemplate_EventWebsiteLink_UsesPublicEventLink()
    {
        var template = BuiltInEmailTemplateCatalog.CreateTemplate(BuiltInEmailTemplateNames.TicketConfirmation);
        var parameters = EmailTemplateParameters.WithBranding(
            new
            {
                FirstName = "Alice",
                TeamName = "Admitto",
                EventName = "DevConf",
                EventWebsite = "https://devconf.example.com",
                PublicEventLink = "https://admitto.example.com/e/devconf",
                QRCodeLink = "https://admitto.example.com/e/devconf/qr-code/registration-id",
                CancelLink = "https://admitto.example.com/e/devconf/cancel/registration-id",
                EditRegistrationLink = "https://admitto.example.com/e/devconf/edit/registration-id",
                TicketTypes = Array.Empty<string>()
            },
            AccentColor.From("#0f766e"),
            EmailFontFamily.From("Arial"));

        var rendered = new ScribanEmailRenderer().Render(template, parameters);

        rendered.HtmlBody.ShouldContain("href=\"https://admitto.example.com/e/devconf\"");
        rendered.HtmlBody.ShouldContain(">our website</a>");
        rendered.HtmlBody.ShouldContain("Modify/Cancel Registration");
        rendered.HtmlBody.ShouldContain("href=\"https://admitto.example.com/e/devconf/edit/registration-id\"");
        rendered.HtmlBody.ShouldNotContain("Cancel My Registration");
        rendered.HtmlBody.ShouldNotContain("https://devconf.example.com");
        rendered.TextBody.ShouldContain("https://admitto.example.com/e/devconf");
        rendered.TextBody.ShouldContain("Modify/Cancel your Registration");
        rendered.TextBody.ShouldContain("https://admitto.example.com/e/devconf/edit/registration-id");
        rendered.TextBody.ShouldNotContain("Cancel your registration:");
        rendered.TextBody.ShouldNotContain("https://devconf.example.com");
    }

    // Given branding parameters with a configured font family and accent color
    // When every built-in email template is rendered
    // Then each rendered template's HTML includes the configured font family and accent color
    [TestMethod]
    public void BuiltInEmailTemplates_RenderConfiguredFontAndAccentColor()
    {
        var renderer = new ScribanEmailRenderer();
        var parameters = EmailTemplateParameters.WithBranding(
            EmailTemplateSampleParameters.Create(),
            AccentColor.From("#0f766e"),
            EmailFontFamily.From("Georgia, serif"));

        foreach (var entry in BuiltInEmailTemplateCatalog.All)
        {
            var template = BuiltInEmailTemplateCatalog.CreateTemplate(entry.Name);

            var rendered = renderer.Render(template, parameters);

            rendered.HtmlBody.Contains("font-family: Georgia, serif")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured font family.");
            rendered.HtmlBody.Contains("#0f766e")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured accent color.");
        }
    }

    // Given branding parameters built with an accent color and font family
    // When the template parameters are constructed
    // Then they expose canonical branding keys without the legacy accent key
    [TestMethod]
    public void EmailTemplateParameters_AccentColorArgument_ExportsCanonicalAccentColor()
    {
        var parameters = EmailTemplateParameters.WithBranding(
            new { FirstName = "Alice" },
            AccentColor.From("#dc2626"),
            EmailFontFamily.From("Arial"));

        parameters["accent_color"].ShouldBe("#dc2626");
        parameters["font_family"].ShouldBe("Arial");
        parameters.ShouldNotContainKey("team_accent_color");
    }
}
