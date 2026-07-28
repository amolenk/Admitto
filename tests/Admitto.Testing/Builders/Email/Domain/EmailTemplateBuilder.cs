using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Testing.Builders.Email.Domain;

public sealed class EmailTemplateBuilder
{
    private const string Name = "Test template";
    private string _subject = "Hello {{ first_name }}";
    private string _textBody = "Hello {{ first_name }}";
    private string _htmlBody = "<p>Hello {{ first_name }}</p>";

    public EmailTemplateBuilder WithSubject(string subject) { _subject = subject; return this; }
    public EmailTemplateBuilder WithTextBody(string textBody) { _textBody = textBody; return this; }
    public EmailTemplateBuilder WithHtmlBody(string htmlBody) { _htmlBody = htmlBody; return this; }

    public EmailTemplate Build() => new (Name, _subject, _textBody, _htmlBody);
}
