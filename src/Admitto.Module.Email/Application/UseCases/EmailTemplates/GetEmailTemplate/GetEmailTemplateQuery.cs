using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

internal sealed record GetEmailTemplateQuery(
    EmailTemplateId Id) : Query<EmailTemplateDto?>;
