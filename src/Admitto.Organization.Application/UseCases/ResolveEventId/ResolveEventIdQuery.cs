using Amolenk.Admitto.Organization.Domain.ValueObjects;
using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.ResolveEventId;

internal record ResolveEventIdQuery(TeamId TeamId, TicketedEventSlug EventSlug) : Query<Guid>;