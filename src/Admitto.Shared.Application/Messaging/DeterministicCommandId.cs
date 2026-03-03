using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Shared.Application.Messaging;

public static class DeterministicCommandId<TCommand>
{
    public static Guid Create(Guid eventId) =>
        DeterministicGuid.Create($"{eventId}:{typeof(TCommand).FullName}");
}