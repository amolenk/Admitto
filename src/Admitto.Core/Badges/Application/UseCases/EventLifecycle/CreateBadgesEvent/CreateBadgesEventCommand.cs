using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.CreateBadgesEvent;

internal sealed record CreateBadgesEventCommand(Guid EventId, Guid TeamId) : Command;
