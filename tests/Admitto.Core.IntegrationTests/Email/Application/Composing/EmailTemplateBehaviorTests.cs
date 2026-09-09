using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;

[TestClass]
public sealed class EmailTemplateBehaviorTests
{
    // Given ticket confirmation values with an event website and public event link
    // When the ticket confirmation template is rendered
    // Then links use the public event link and show edit-registration wording
    [TestMethod]
    public void TicketConfirmationTemplate_EventWebsiteLink_UsesPublicEventLink()
    {
        var template = BuiltInEmailTemplateCatalog.CreateTemplate(BuiltInEmailTemplateNames.TicketConfirmation);
        var values = Values();
        values["event_name"] = "DevConf";
        values["event_website"] = "https://devconf.example.com";
        values["public_event_link"] = "https://admitto.example.com/e/devconf";
        values["qrcode_link"] = "https://admitto.example.com/e/devconf/qr-code/registration-id";
        values["cancel_link"] = "https://admitto.example.com/e/devconf/cancel/registration-id";
        values["edit_registration_link"] = "https://admitto.example.com/e/devconf/edit/registration-id";
        var rendered = new ScribanEmailRenderer().Render(template, values);

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

    // Given explicit branding values
    // When every built-in email template is rendered
    // Then each rendered template includes the configured font and accent
    [TestMethod]
    public void BuiltInEmailTemplates_RenderConfiguredFontAndAccentColor()
    {
        var renderer = new ScribanEmailRenderer();
        var values = Values();
        values["accent_color"] = "#0f766e";
        values["font_family"] = "Georgia, serif";

        foreach (var entry in BuiltInEmailTemplateCatalog.All)
        {
            var rendered = renderer.Render(
                BuiltInEmailTemplateCatalog.CreateTemplate(entry.Name), values);
            rendered.HtmlBody.Contains("font-family: Georgia, serif")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured font family.");
            rendered.HtmlBody.Contains("#0f766e")
                .ShouldBeTrue($"Built-in template '{entry.Name}' must render the configured accent color.");
        }
    }

    private static Dictionary<string, object?> Values() => new(StringComparer.OrdinalIgnoreCase)
    {
        ["first_name"] = "Alice",
        ["team_name"] = "Admitto",
        ["accent_color"] = AccentColor.Default,
        ["font_family"] = "Arial",
        ["event_name"] = "DevConf",
        ["event_website"] = "https://example.com",
        ["public_event_link"] = "https://example.com/event",
        ["qrcode_link"] = "https://example.com/qrcode",
        ["cancel_link"] = "https://example.com/cancel",
        ["edit_registration_link"] = "https://example.com/edit",
        ["register_link"] = "https://example.com/register",
        ["reconfirm_link"] = "https://example.com/reconfirm",
        ["ticket_types"] = new[] { "Conference Pass" },
        ["plain_code"] = "123456",
        ["coupon_code"] = "WAITLIST-ABC123",
        ["ticket_type_name"] = "Conference Pass",
        ["expires_at"] = "2026-06-15 08:00 (Europe/Amsterdam)"
    };
}
