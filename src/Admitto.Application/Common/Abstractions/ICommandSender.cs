namespace Amolenk.Admitto.Application.Common.Abstractions;

public interface ICommandSender
{
    ValueTask SendAsync(ICommand command);
}