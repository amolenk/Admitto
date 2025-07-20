using Amolenk.Admitto.Domain.Entities;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Jobs.SendEmail;

public record SendEmailJobData(
    Guid JobId,
    Guid TeamId,
    Guid TicketedEventId,
    EmailType EmailType,
    Guid DataEntityId,
    string? RecipientEmail = null)
    : IJobData;
    
    
    