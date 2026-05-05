using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.CreateCustomBulkTemplate;

internal sealed record CreateCustomBulkTemplateCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Name,
    string Subject,
    string TextBody,
    string? HtmlBody) : Command<EmailTemplateId>;
