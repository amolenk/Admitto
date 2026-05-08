namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailTemplates.TestSendEmailTemplate.AdminApi;

public sealed record TestSendEmailTemplateHttpRequest(string Recipient)
{
    internal TestSendEmailTemplateCommand ToCommand(
        Guid templateId,
        Guid teamId,
        Guid? eventId) =>
        new(templateId, teamId, eventId, Recipient);
}
