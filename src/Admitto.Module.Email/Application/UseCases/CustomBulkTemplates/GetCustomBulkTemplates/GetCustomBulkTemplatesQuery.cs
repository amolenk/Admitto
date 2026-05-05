using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplates;

internal sealed record GetCustomBulkTemplatesQuery(EmailSettingsScope Scope, Guid ScopeId) : Query<IReadOnlyList<CustomBulkTemplateListItemDto>>;
