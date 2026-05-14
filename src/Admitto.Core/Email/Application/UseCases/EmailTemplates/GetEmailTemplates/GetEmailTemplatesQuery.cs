using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

internal sealed record GetEmailTemplatesQuery(
    EmailSettingsScope Scope,
    EmailScopeId ScopeId,
    EmailScopeId? ParentScopeId = null) : Query<IReadOnlyList<EmailTemplateListItemDto>>;
