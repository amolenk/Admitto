using Amolenk.Admitto.Module.Shared.Application.Messaging;
using Amolenk.Admitto.Module.Shared.Contracts;
using Amolenk.Admitto.Module.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Module.Email.Application.Messaging;

/// <summary>
/// Email module's message policy. The Email module currently has no domain events that need to be
/// republished as module or integration events; the class is registered for symmetry with the other
/// modules and so that future events can be added without touching DI wiring.
/// </summary>
public sealed class EmailMessagePolicy : MessagePolicy
{
}
