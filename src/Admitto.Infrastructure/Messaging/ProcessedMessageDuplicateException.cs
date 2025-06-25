namespace Amolenk.Admitto.Infrastructure.Messaging;

/// <summary>
/// Exception thrown when a message has already been processed by another instance.
/// This is used to implement exactly-once message processing.
/// </summary>
public class ProcessedMessageDuplicateException : Exception
{
    public ProcessedMessageDuplicateException()
    {
    }

    public ProcessedMessageDuplicateException(string message) : base(message)
    {
    }

    public ProcessedMessageDuplicateException(string message, Exception innerException) : base(message, innerException)
    {
    }
}