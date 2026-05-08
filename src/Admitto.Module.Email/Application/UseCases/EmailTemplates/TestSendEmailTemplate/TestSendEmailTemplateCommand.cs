using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed record TestSendEmailTemplateCommand(
    Guid TemplateId,
    Guid TeamId,
    Guid? EventId,
    string Recipient) : Command;
