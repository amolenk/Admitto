using Amolenk.Admitto.Core.Email.Domain.ValueObjects;

namespace Amolenk.Admitto.Core.Email.Application.UseCases.EmailSettings.SendTestEmail.AdminApi;

public sealed record SendTestEmailHttpRequest(string Recipient)
{
    internal SendTestEmailCommand ToCommand(Guid teamId, Guid? ticketedEventId) =>
        new(teamId, ticketedEventId, Recipient);
}
