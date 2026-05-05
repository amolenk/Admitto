using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate;

internal sealed record TestSendEmailTemplateCommand(
    TeamId TeamId,
    TicketedEventId? EventId,
    string Type,
    EmailAddress Recipient) : Command;
