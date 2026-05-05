using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.DeleteCustomBulkTemplate;

internal sealed class DeleteCustomBulkTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<DeleteCustomBulkTemplateCommand>
{
    public async ValueTask HandleAsync(
        DeleteCustomBulkTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var template = await writeStore.EmailTemplates
            .FindAsync([command.Id], cancellationToken);

        if (template is null || template.Type != EmailTemplateType.BulkCustom)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.EmailTemplate>(command.Id.ToString()));

        writeStore.EmailTemplates.Remove(template);
    }
}
