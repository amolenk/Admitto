using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Badges.Application.UseCases.EventLifecycle.ArchiveBadgesEvent;

internal sealed record ArchiveBadgesEventCommand(Guid EventId) : Command;
