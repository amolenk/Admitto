using Amolenk.Admitto.Shared.Application.Messaging;
using Amolenk.Admitto.Shared.Kernel.ValueObjects;

namespace Amolenk.Admitto.Organization.Application.UseCases.GetEventId;

internal record GetEventIdQuery(TeamId TeamId, Slug EventSlug) : Query<TicketedEventId>;