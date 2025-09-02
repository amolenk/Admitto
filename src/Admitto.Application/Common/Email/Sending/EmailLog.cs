using Amolenk.Admitto.Domain.Contracts;
using Amolenk.Admitto.Domain.ValueObjects;

namespace Amolenk.Admitto.Application.Common.Email.Sending;

/// <summary>
/// Tracks sent emails to prevent duplication.
/// </summary>
public class EmailLog
{
    public required Guid Id { get; init; }
    
    public required Guid TeamId { get; init; }

    public required Guid TicketedEventId { get; init; }

    public required Guid IdempotencyKey { get; init; }
    
    public required string Recipient { get; init; }
    
    public required string EmailType { get; init; }
    
    public required string Subject { get; init; }
    
    public required string Provider { get; init; }
    
    public string? ProviderMessageId { get; init; }
    
    public required EmailStatus Status { get; init; }
    
    public DateTimeOffset? SentAt { get; set; }
    
    public required DateTimeOffset StatusUpdatedAt { get; set; }

    public string? LastError { get; set; }
}

public enum EmailStatus
{
    Sent,
    Delivered,
    Bounced,
    Failed
}