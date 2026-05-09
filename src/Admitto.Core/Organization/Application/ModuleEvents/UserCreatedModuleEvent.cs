using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Organization.Application.ModuleEvents;

public sealed record UserCreatedModuleEvent(Guid UserId) : ModuleEvent;
