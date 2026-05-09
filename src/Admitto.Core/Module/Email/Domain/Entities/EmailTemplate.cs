using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Kernel.Entities;

namespace Amolenk.Admitto.Core.Module.Email.Domain.Entities;

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
        string name,
        string subject,
        string textBody,
        string? htmlBody)
        : base(id)
    {
        Scope = scope;
        ScopeId = scopeId;
        Name = name;
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public EmailSettingsScope Scope { get; private set; }
    public Guid ScopeId { get; private set; }

    /// <summary>
    /// The template's unique name within its scope.
    /// Names matching <see cref="BuiltInEmailTemplateNames"/> are reserved for built-in templates
    /// and may not be used for custom templates or renamed.
    /// </summary>
    public string Name { get; private set; } = default!;

    public string Subject { get; private set; } = default!;
    public string TextBody { get; private set; } = default!;
    public string? HtmlBody { get; private set; }

    public static EmailTemplate Create(
        EmailSettingsScope scope,
        Guid scopeId,
        string name,
        string subject,
        string textBody,
        string? htmlBody)
    {
        return new EmailTemplate(
            EmailTemplateId.New(),
            scope,
            scopeId,
            name,
            subject,
            textBody,
            htmlBody);
    }

    public void Update(string subject, string textBody, string? htmlBody)
    {
        Subject = subject;
        TextBody = textBody;
        HtmlBody = htmlBody;
    }

    public void Rename(string newName)
    {
        Name = newName;
    }
}
