using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IEmailSender
{
    Task SendEmailAsync(string recipientEmail, string subject, string body, TeamId teamId, 
        TicketedEventId? ticketedEventId = null, AttendeeId? attendeeId = null);
}
