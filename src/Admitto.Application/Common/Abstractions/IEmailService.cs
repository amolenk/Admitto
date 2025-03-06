namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailService
{
    Task SendAcceptanceEmailAsync();
    Task SendRejectionEmailAsync(Guid attendeeId, Guid ticketedEventId);
}