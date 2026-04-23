using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate;

/// <summary>
/// Creates or updates an <see cref="EmailTemplate"/> for a given scope and type.
/// When <see cref="UpsertEmailTemplateCommand.Version"/> is null, a new template is created (uniqueness
/// enforced by <c>IX_email_templates_scope_scope_id_type</c>). When supplied, updates with optimistic
/// concurrency.
/// </summary>
internal sealed class UpsertEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<UpsertEmailTemplateCommand>
{
    public async ValueTask HandleAsync(UpsertEmailTemplateCommand command, CancellationToken cancellationToken)
    {
        if (command.Version is null)
        {
            var template = EmailTemplate.Create(
                command.Scope,
                command.ScopeId,
                command.Type,
                command.Subject,
                command.TextBody,
                command.HtmlBody);

            writeStore.EmailTemplates.Add(template);
            return;
        }

        var existing = await writeStore.EmailTemplates
            .FirstOrDefaultAsync(
                t => t.Scope == command.Scope && t.ScopeId == command.ScopeId && t.Type == command.Type,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<EmailTemplate>(command.Type));

        if (command.Version != existing.Version)
        {
            throw new BusinessRuleViolationException(
                CommonErrors.ConcurrencyConflict(command.Version.Value, existing.Version));
        }

        existing.Update(command.Subject, command.TextBody, command.HtmlBody);
    }
}
