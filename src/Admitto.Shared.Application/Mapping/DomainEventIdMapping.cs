using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Application.Mapping;

public static class DomainEventIdMapping
{
    public static CommandId ToDeterministicCommandId<TCommand>(this DomainEventId domainEventId)
        where TCommand : ICommand
        => CommandId.From(DeterministicGuid.Create($"{domainEventId.Value}:{typeof(TCommand).FullName}"));
}