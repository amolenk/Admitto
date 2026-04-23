using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed class DeleteEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteEmailTemplateCommand>
{
    public async ValueTask HandleAsync(DeleteEmailTemplateCommand command, CancellationToken cancellationToken)
    {
        var template = await writeStore.EmailTemplates
            .FirstOrDefaultAsync(
                t => t.Scope == command.Scope && t.ScopeId == command.ScopeId && t.Type == command.Type,
                cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<EmailTemplate>(command.Type));

        if (command.ExpectedVersion != template.Version)
        {
            throw new BusinessRuleViolationException(
                CommonErrors.ConcurrencyConflict(command.ExpectedVersion, template.Version));
        }

        writeStore.EmailTemplates.Remove(template);
    }
}
