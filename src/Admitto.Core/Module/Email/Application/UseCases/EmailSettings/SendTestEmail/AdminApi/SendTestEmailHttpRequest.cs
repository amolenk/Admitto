using Amolenk.Admitto.Core.Module.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Module.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public sealed record SendTestEmailHttpRequest(string Recipient)
{
    internal SendTestEmailCommand ToCommand(EmailSettingsScope scope, Guid scopeId) =>
        new(scope, scopeId, Recipient);
}
