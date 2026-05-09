using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed record TestSendEmailTemplateCommand(
    Guid TemplateId,
    Guid TeamId,
    Guid? EventId,
    string Recipient) : Command;
