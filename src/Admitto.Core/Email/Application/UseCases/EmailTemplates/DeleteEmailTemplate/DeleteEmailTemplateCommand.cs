using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailTemplates.DeleteEmailTemplate;

internal sealed record DeleteEmailTemplateCommand(
    Guid Id,
    Guid TeamId,
    Guid? TicketedEventId,
    uint ExpectedVersion) : Command;
