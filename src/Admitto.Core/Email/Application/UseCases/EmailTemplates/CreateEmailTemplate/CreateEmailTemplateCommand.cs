using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.CreateEmailTemplate;

internal sealed record CreateEmailTemplateCommand(
    EmailSettingsScope Scope,
    EmailScopeId ScopeId,
    string Name,
    string? Subject,
    string? TextBody,
    string? HtmlBody,
    EmailScopeId? ParentScopeId = null) : Command<Guid>;
