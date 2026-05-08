using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

internal sealed record CreateEmailTemplateCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    Guid? ParentScopeId = null) : Command<Guid>;
