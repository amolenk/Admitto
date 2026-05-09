using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

internal sealed record CreateEmailTemplateCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    Guid? ParentScopeId = null) : Command<Guid>;
