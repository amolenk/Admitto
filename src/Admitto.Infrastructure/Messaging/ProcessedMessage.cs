namespace Amolenk.Admitto.Infrastructure.Messaging;

/// <summary>
/// Entity to track processed messages for exactly-once processing.
/// </summary>
public class ProcessedMessage
{
    public Guid MessageId { get; private set; }
    public DateTime ProcessedAt { get; private set; }

    /// <summary>
    /// Parameterless constructor for EF Core.
    /// </summary>
    private ProcessedMessage()
    {
    }

    public ProcessedMessage(Guid messageId)
    {
        MessageId = messageId;
        ProcessedAt = DateTime.UtcNow;
    }
}