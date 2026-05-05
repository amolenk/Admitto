using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.CustomBulkTemplates.GetCustomBulkTemplate;

internal sealed record GetCustomBulkTemplateQuery(EmailTemplateId Id) : Query<CustomBulkTemplateDto?>;
