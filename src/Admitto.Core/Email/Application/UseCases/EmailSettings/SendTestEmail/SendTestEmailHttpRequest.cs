namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailHttpRequest(
    string Recipient)
{
    public SendTestEmailCommand ToCommand(
        Guid teamId,
        Guid? ticketedEventId)
    {
        return new SendTestEmailCommand(
            teamId,
            ticketedEventId,
            Recipient);
    }
}
