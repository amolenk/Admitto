using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed class DeleteEmailTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteEmailTemplateCommand>
{
    public async ValueTask HandleAsync(DeleteEmailTemplateCommand command, CancellationToken cancellationToken)
    {
        EmailTemplateId id = EmailTemplateId.From(command.Id);

        var template = await writeStore.EmailTemplates
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<EmailTemplate>(id.Value.ToString()));

        if (command.ExpectedVersion != template.Version)
        {
            throw new BusinessRuleViolationException(
                CommonErrors.ConcurrencyConflict(command.ExpectedVersion, template.Version));
        }

        writeStore.EmailTemplates.Remove(template);
    }
}
