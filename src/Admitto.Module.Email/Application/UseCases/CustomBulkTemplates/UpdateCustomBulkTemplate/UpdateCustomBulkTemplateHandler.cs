using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.UpdateCustomBulkTemplate;

internal sealed class UpdateCustomBulkTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<UpdateCustomBulkTemplateCommand>
{
    public async ValueTask HandleAsync(
        UpdateCustomBulkTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var template = await writeStore.EmailTemplates
            .FindAsync([command.Id], cancellationToken)
            ?? throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.EmailTemplate>(command.Id.ToString()));

        if (template.Type != EmailTemplateType.BulkCustom)
            throw new BusinessRuleViolationException(
                NotFoundError.Create<Domain.Entities.EmailTemplate>(command.Id.ToString()));

        if (command.Version != template.Version)
        {
            throw new BusinessRuleViolationException(
                CommonErrors.ConcurrencyConflict(command.Version, template.Version));
        }

        var nameLower = command.Name.ToLowerInvariant();
        var nameConflict = await writeStore.EmailTemplates
            .AnyAsync(
                t => t.Id != command.Id
                     && t.Scope == template.Scope
                     && t.ScopeId == template.ScopeId
                     && t.Type == EmailTemplateType.BulkCustom
                     && EF.Functions.ILike(t.Name!, nameLower),
                cancellationToken);

        if (nameConflict)
            throw new BusinessRuleViolationException(
                AlreadyExistsError.Create<Domain.Entities.EmailTemplate>());

        template.Update(command.Subject, command.TextBody, command.HtmlBody, command.Name);
    }
}

