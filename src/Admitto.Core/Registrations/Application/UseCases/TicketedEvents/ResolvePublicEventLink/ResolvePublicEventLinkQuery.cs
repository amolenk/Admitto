using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.TicketedEvents.ResolvePublicEventLink;

internal sealed record ResolvePublicEventLinkQuery(string PublicSlug) : Query<PublicEventLinkDto?>;

public sealed record PublicEventLinkDto(string PublicSlug, string WebsiteUrl);
