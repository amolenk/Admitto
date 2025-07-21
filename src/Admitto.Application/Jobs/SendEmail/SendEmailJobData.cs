using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public record SendEmailJobData(
    Guid JobId,
    Guid TeamId,
    Guid TicketedEventId,
    Guid DataEntityId,
    EmailType EmailType,
    string? RecipientEmail = null)
    : IJobData;
    
    
    