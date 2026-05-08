using Amolenk.Admitto.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EmailSettings.SendTestEmail;

internal sealed record SendTestEmailHttpRequest(
    string Recipient)
{
    public SendTestEmailCommand ToCommand(
        EmailSettingsScope scope,
        Guid scopeId)
    {
        return new SendTestEmailCommand(
            scope,
            scopeId,
            Recipient);
    }
}