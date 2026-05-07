using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed record TestSendEmailTemplateCommand(
    EmailTemplateId TemplateId,
    TeamId TeamId,
    TicketedEventId? EventId,
    EmailAddress Recipient) : Command;
