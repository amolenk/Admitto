namespace Amolenk.Admitto.Application.Common.Abstractions;

/// <summary>
/// Marker interface for command and domain event handlers that require exactly-once processing.
/// Handlers implementing this interface will have their processed messages tracked to prevent duplicate execution.
/// </summary>
public interface IProcessMessagesExactlyOnce
{
}