using Amolenk.Admitto.Module.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Module.Shared.Application.Messaging;

public static class DeterministicCommandId<TCommand>
{
    public static Guid Create(Guid eventId) =>
        DeterministicGuid.Create($"{eventId}:{typeof(TCommand).FullName}");
}