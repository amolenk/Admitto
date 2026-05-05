using Amolenk.Admitto.Module.Email.Application.Persistence;
using Amolenk.Admitto.Module.Email.Domain.Entities;
using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ErrorHandling;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate;

internal sealed class CreateCustomBulkTemplateHandler(IEmailWriteStore writeStore)
    : ICommandHandler<CreateCustomBulkTemplateCommand, EmailTemplateId>
{
    public async ValueTask<EmailTemplateId> HandleAsync(
        CreateCustomBulkTemplateCommand command,
        CancellationToken cancellationToken)
    {
        var nameLower = command.Name.ToLowerInvariant();
        var nameExists = await writeStore.EmailTemplates
            .AnyAsync(
                t => t.Scope == command.Scope
                     && t.ScopeId == command.ScopeId
                     && t.Type == EmailTemplateType.BulkCustom
                     && EF.Functions.ILike(t.Name!, nameLower),
                cancellationToken);

        if (nameExists)
            throw new BusinessRuleViolationException(
                AlreadyExistsError.Create<EmailTemplate>());

        var template = EmailTemplate.Create(
            command.Scope,
            command.ScopeId,
            EmailTemplateType.BulkCustom,
            command.Subject,
            command.TextBody,
            command.HtmlBody,
            command.Name);

        writeStore.EmailTemplates.Add(template);
        return template.Id;
    }
}
