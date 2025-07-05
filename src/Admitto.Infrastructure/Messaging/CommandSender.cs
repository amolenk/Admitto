using Amolenk.Admitto.Application.Common.Abstractions;

namespace Amolenk.Admitto.Infrastructure.Messaging;

public class CommandSender : ICommandSender
{
    public ValueTask SendAsync(ICommand command)
    {
        throw new NotImplementedException();
    }
}