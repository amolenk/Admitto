using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public sealed record TestSendEmailTemplateHttpRequest(string Recipient)
{
    internal TestSendEmailTemplateCommand ToCommand(
        TeamId teamId,
        TicketedEventId? eventId,
        string type) =>
        new(teamId, eventId, type, EmailAddress.From(Recipient));
}
