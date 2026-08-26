using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Testing.Builders.Email.Domain;
using Shouldly;

namespace Amolenk.Admitto.Core.Email.Domain.Tests.Templating;

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

        var result = _renderer.Render(template, new { FirstName = "Alice", EventName = "DevConf 2026" });

        result.Subject.ShouldBe("Hello Alice");
        result.TextBody.ShouldBe("Your event: DevConf 2026");
        result.HtmlBody.ShouldBe("<p>Your event: DevConf 2026</p>");
    }

    // Given a template referencing variables that are not present in the model
    // When the template is rendered with an empty model
    // Then the missing variables are rendered as blank
    [TestMethod]
    public void Render_MissingVariable_LeavesBlank()
    {
        var template = new EmailTemplateBuilder()
            .WithSubject("Hi {{ first_name }}")
            .WithTextBody("Event: {{ event_name }}")
            .WithHtmlBody("<b>{{ event_name }}</b>")
            .Build();

        var result = _renderer.Render(template, new { });

        result.Subject.ShouldBe("Hi ");
        result.TextBody.ShouldBe("Event: ");
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

        Should.Throw<EmailRenderException>(() => _renderer.Render(template, new { }));
    }
}
