using Amolenk.Admitto.Module.Email.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public sealed record TestSendEmailTemplateHttpRequest(string Recipient)
{
    internal TestSendEmailTemplateCommand ToCommand(
        EmailTemplateId templateId,
        TeamId teamId,
        TicketedEventId? eventId) =>
        new(templateId, teamId, eventId, EmailAddress.From(Recipient));
}
