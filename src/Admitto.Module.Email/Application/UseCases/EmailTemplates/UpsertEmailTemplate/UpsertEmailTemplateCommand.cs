using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.UpsertEmailTemplate;

internal sealed record UpsertEmailTemplateCommand(
    EmailSettingsScope Scope,
    Guid ScopeId,
    string Type,
    string Subject,
    string TextBody,
    string HtmlBody,
    uint? Version) : Command;
