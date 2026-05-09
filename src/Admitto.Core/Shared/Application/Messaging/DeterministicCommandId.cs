using Amolenk.Admitto.Core.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Core.Shared.Application.Messaging;

public static class DeterministicCommandId<TCommand>
{
    public static Guid Create(Guid eventId) =>
        DeterministicGuid.Create($"{eventId}:{typeof(TCommand).FullName}");
}