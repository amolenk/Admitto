namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IMessageSender
{
    ValueTask SendAsync(Message message, CancellationToken cancellationToken = default);
}