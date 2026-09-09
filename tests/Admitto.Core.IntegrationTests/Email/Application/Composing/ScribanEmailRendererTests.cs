using Amolenk.Admitto.Core.Email.Application.Composing;
using Amolenk.Admitto.Testing.Builders.Email.Domain;

namespace Amolenk.Admitto.Core.IntegrationTests.Email.Application.Composing;

[TestClass]
public sealed class ScribanEmailRendererTests
{
    private readonly ScribanEmailRenderer _renderer = new();

    // Given a template whose subject and bodies reference model variables
    // When the template is rendered with matching data
    // Then the variables are substituted into the subject, text body, and HTML body
    [TestMethod]
    public void Render_ValidTemplate_SubstitutesVariables()
    {
        var template = new EmailTemplateBuilder()
            .WithSubject("Hello {{ first_name }}")
            .WithTextBody("Your event: {{ event_name }}")
            .WithHtmlBody("<p>Your event: {{ event_name }}</p>")
            .Build();

        var result = _renderer.Render(template, new Dictionary<string, object?>
        {
            ["first_name"] = "Alice",
            ["event_name"] = "DevConf 2026"
        });

        result.Subject.ShouldBe("Hello Alice");
        result.TextBody.ShouldBe("Your event: DevConf 2026");
        result.HtmlBody.ShouldBe("<p>Your event: DevConf 2026</p>");
    }

    // Given a template with an invalid Scriban expression in the subject
    // When the template is rendered
    // Then it throws an EmailRenderException
    [TestMethod]
    public void Render_ParseError_ThrowsEmailRenderException()
    {
        var template = new EmailTemplateBuilder()
            .WithSubject("{{ for }}")
            .WithTextBody("body")
            .WithHtmlBody("<p>body</p>")
            .Build();

        Should.Throw<EmailRenderException>(() => _renderer.Render(
            template, new Dictionary<string, object?>()));
    }

    // Given a template referencing variables that are not present in the model
    // When the template is rendered with an empty model
    // Then it throws an EmailRenderException
    [TestMethod]
    public void Render_MissingVariable_ThrowsEmailRenderException()
    {
        var template = new EmailTemplateBuilder()
            .WithSubject("Hi {{ first_name }}")
            .WithTextBody("Event: {{ event_name }}")
            .WithHtmlBody("<b>{{ event_name }}</b>")
            .Build();

        Should.Throw<EmailRenderException>(() => _renderer.Render(
            template, new Dictionary<string, object?>()));
    }
}
