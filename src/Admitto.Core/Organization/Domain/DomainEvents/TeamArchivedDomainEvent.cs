using Amolenk.Admitto.Core.Shared.Kernel.DomainEvents;

namespace Amolenk.Admitto.Core.Organization.Domain.DomainEvents;

public sealed record TeamArchivedDomainEvent(TeamId TeamId) : DomainEvent;
