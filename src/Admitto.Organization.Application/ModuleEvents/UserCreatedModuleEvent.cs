using Amolenk.Admitto.Shared.Application.Messaging;

namespace Amolenk.Admitto.Organization.Application.ModuleEvents;

public sealed record UserCreatedModuleEvent(Guid UserId) : ModuleEvent;
