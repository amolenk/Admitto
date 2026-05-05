using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Module.Email.Domain.Entities;

public class EmailTemplate : Aggregate<EmailTemplateId>
{
    // Required for EF Core
    private EmailTemplate()
    {
    }

    private EmailTemplate(
        EmailTemplateId id,
        EmailSettingsScope scope,
        Guid scopeId,
        string type,
        string? name,
        string subject,
        string textBody,
        string? htmlBody)
        : base(id)
    {
        Scope = scope;
        ScopeId = scopeId;
        Type = type;
        Name = name;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public EmailSettingsScope Scope { get; private set; }
    public Guid ScopeId { get; private set; }
    public string Type { get; private set; } = default!;
    public string? Name { get; private set; }
    public string Subject { get; private set; } = default!;
    public string TextBody { get; private set; } = default!;
    public string? HtmlBody { get; private set; }

    public static EmailTemplate Create(
        EmailSettingsScope scope,
        Guid scopeId,
        string type,
        string subject,
        string textBody,
        string? htmlBody,
        string? name = null)
    {
        return new EmailTemplate(
            EmailTemplateId.New(),
            scope,
            scopeId,
            type,
            name,
            subject,
            textBody,
            htmlBody);
    }

    public void Update(string subject, string textBody, string? htmlBody, string? name = null)
    {
        Name = name;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }
}
