using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Application.ModuleEvents;

public sealed record UserCreatedModuleEvent(Guid UserId) : ModuleEvent;
