using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Organization.Application.ModuleEvents;

public sealed record UserCreatedModuleEvent(Guid UserId) : ModuleEvent;
