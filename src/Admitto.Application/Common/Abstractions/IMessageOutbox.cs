namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface IMessageOutbox
{
    void EnqueueCommand(ICommand command, bool priority = false);
}