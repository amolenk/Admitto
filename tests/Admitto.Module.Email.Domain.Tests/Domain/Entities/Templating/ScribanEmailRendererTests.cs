using Amolenk.Admitto.Module.Email.Application.Templating;
using Amolenk.Admitto.Module.Email.Domain.Tests.Builders;
using Shouldly;

namespace Amolenk.Admitto.Module.Email.Domain.Tests.Templating;

[TestClass]
public sealed class ScribanEmailRendererTests
{
    private readonly ScribanEmailRenderer _renderer = new();

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
