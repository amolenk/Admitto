using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

internal sealed record GetEmailTemplatesQuery(
    EmailSettingsScope Scope,
    Guid ScopeId,
    Guid? ParentScopeId = null) : Query<IReadOnlyList<EmailTemplateListItemDto>>;
