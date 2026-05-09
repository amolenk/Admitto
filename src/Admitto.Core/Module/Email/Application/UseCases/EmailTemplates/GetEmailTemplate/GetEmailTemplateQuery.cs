using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailTemplates.GetEmailTemplate;

internal sealed record GetEmailTemplateQuery(
    EmailTemplateId Id) : Query<EmailTemplateDto?>;
