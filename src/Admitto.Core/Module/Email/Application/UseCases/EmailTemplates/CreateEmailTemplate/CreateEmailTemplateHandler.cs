using Amolenk.Admitto.Core.Module.Email.Application.Persistence;
using Amolenk.Admitto.Core.Module.Email.Application.Templating;
using Amolenk.Admitto.Core.Module.Email.Domain.Entities;
using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

internal sealed class CreateEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<CreateEmailTemplateCommand, Guid>
{
    private static readonly Error ReservedNameError = new(
        "email_template.reserved_name",
        "This name is reserved for a built-in template. You cannot create a custom template with this name.");

    private static readonly Error AlreadyExistsError = new(
        "email_template.already_exists",
        "A template with this name already exists in this scope.");

    public async ValueTask<Guid> HandleAsync(
        CreateEmailTemplateCommand command,
        CancellationToken ct)
    {
        var isBuiltIn = BuiltInEmailTemplateNames.IsReserved(command.Name);

        if (!isBuiltIn && BuiltInEmailTemplateNames.IsReserved(command.Name))
            throw new BusinessRuleViolationException(ReservedNameError);

        var alreadyExists = await writeStore.EmailTemplates.AnyAsync(
            t => t.Scope == command.Scope &&
                 t.ScopeId == command.ScopeId &&
                 t.Name.ToLower() == command.Name.ToLower(),
            ct);

        if (alreadyExists)
            throw new BusinessRuleViolationException(AlreadyExistsError);

        string subject;
        string textBody;
        string? htmlBody;

        if (isBuiltIn)
        {
            var catalogEntry = BuiltInEmailTemplateCatalog.GetByName(command.Name)!;
            subject = command.Subject ?? catalogEntry.DefaultSubject;
            textBody = command.TextBody ?? catalogEntry.DefaultTextBody;
            htmlBody = command.HtmlBody ?? catalogEntry.DefaultHtmlBody;
        }
        else
        {
            string? parentSubject = null, parentTextBody = null, parentHtmlBody = null;

            if (command.ParentScopeId.HasValue)
            {
                var parentTemplate = await writeStore.EmailTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.ScopeId == command.ParentScopeId.Value &&
                             t.Name.ToLower() == command.Name.ToLower(),
                        ct);

                parentSubject = parentTemplate?.Subject;
                parentTextBody = parentTemplate?.TextBody;
                parentHtmlBody = parentTemplate?.HtmlBody;
            }

            subject = command.Subject ?? parentSubject ?? command.Name;
            textBody = command.TextBody ?? parentTextBody ?? $"Hi,\n\nWe'd like to reach out to you.\n\nBest regards,\nThe team";
            htmlBody = command.HtmlBody ?? parentHtmlBody;
        }

        var template = EmailTemplate.Create(
            command.Scope,
            command.ScopeId,
            command.Name,
            subject,
            textBody,
            htmlBody);

        writeStore.EmailTemplates.Add(template);

        return template.Id.Value;
    }
}
