using Amolenk.Admitto.Core.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

internal sealed record GetEmailTemplateQuery(
    EmailTemplateId Id) : Query<EmailTemplateDto?>;
