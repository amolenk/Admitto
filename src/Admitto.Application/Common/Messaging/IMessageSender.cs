namespace Amolenk.Admitto.Application.Common.Messaging;

public interface IMessageSender
{
    ValueTask SendAsync(Message message, CancellationToken cancellationToken = default);
}