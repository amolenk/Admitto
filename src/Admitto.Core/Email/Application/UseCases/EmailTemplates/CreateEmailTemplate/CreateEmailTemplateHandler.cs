using Amolenk.Admitto.Core.Email.Application.Persistence;
using Amolenk.Admitto.Core.Email.Application.Templating;
using Amolenk.Admitto.Core.Email.Domain.Entities;
using Amolenk.Admitto.Core.Shared.Application.Messaging;
using Amolenk.Admitto.Core.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

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
        var catalogEntry = BuiltInEmailTemplateCatalog.GetByName(command.Name);
        var isBuiltIn = catalogEntry is not null;
        var teamId = TeamId.From(command.TeamId);
        TicketedEventId? ticketedEventId = command.TicketedEventId.HasValue
            ? TicketedEventId.From(command.TicketedEventId.Value)
            : null;

        if (catalogEntry is { IsCustomizable: false })
            throw new BusinessRuleViolationException(ReservedNameError);

        var alreadyExists = await writeStore.EmailTemplates.AnyAsync(
            t => t.TeamId == teamId &&
                 t.TicketedEventId == ticketedEventId &&
                 t.Name.ToLower() == command.Name.ToLower(),
            ct);

        if (alreadyExists)
            throw new BusinessRuleViolationException(AlreadyExistsError);

        string subject;
        string textBody;
        string? htmlBody;

        if (isBuiltIn)
        {
            var entry = catalogEntry ?? throw new InvalidOperationException("Built-in template entry was not found.");
            subject = command.Subject ?? entry.DefaultSubject;
            textBody = command.TextBody ?? entry.DefaultTextBody;
            htmlBody = command.HtmlBody ?? entry.DefaultHtmlBody;
        }
        else
        {
            string? parentSubject = null, parentTextBody = null, parentHtmlBody = null;

            if (ticketedEventId.HasValue)
            {
                var parentTemplate = await writeStore.EmailTemplates
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        t => t.TeamId == teamId &&
                             t.TicketedEventId == null &&
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
            teamId,
            ticketedEventId,
            command.Name,
            subject,
            textBody,
            htmlBody);

        writeStore.EmailTemplates.Add(template);

        return template.Id.Value;
    }
}
