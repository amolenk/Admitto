namespace Amolenk.Admitto.Application.Abstractions;

public interface IEmailService
{
    Task SendAcceptanceEmailAsync();
    Task SendRejectionEmailAsync(Guid attendeeId, Guid ticketedEventId);
}