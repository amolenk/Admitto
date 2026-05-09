using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplates;

internal sealed record GetEmailTemplatesQuery(
    EmailSettingsScope Scope,
    Guid ScopeId,
    Guid? ParentScopeId = null) : Query<IReadOnlyList<EmailTemplateListItemDto>>;
