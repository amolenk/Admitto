namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailHttpRequest(
    string Recipient)
{
    public SendTestEmailCommand ToCommand(
        Guid teamId)
    {
        return new SendTestEmailCommand(
            teamId,
            Recipient);
    }
}
