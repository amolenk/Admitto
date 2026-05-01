using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts.IntegrationEvents;

/// <summary>
/// Published by the Organization module when a team owner has requested the
/// creation of a new ticketed event and Organization has accepted the request
/// (team is not archived, pending counter incremented). The Registrations module
/// consumes this event to materialise the authoritative <c>TicketedEvent</c>
/// aggregate and its <c>TicketCatalog</c>.
/// </summary>
/// <param name="CreationRequestId">
/// Surrogate id assigned by Organization. Used to correlate the eventual
/// <c>TicketedEventCreated</c> or <c>TicketedEventCreationRejected</c> response
/// back to the originating <c>TeamEventCreationRequest</c>.
/// </param>
/// <param name="TeamId">Owning team.</param>
/// <param name="TeamSlug">Slug of the owning team. Denormalised onto the materialised
/// <c>TicketedEvent</c> so registration-bound URL composition (QR codes, signed links)
/// stays inside the Registrations module without an Organization facade lookup.</param>
/// <param name="Slug">Requested event slug (uniqueness is checked by Registrations).</param>
/// <param name="Name">Display name.</param>
/// <param name="WebsiteUrl">Public event website URL.</param>
/// <param name="BaseUrl">Base URL used for event links (QR codes, cancellations, etc.).</param>
/// <param name="StartsAt">Event start (UTC).</param>
/// <param name="EndsAt">Event end (UTC).</param>
/// <param name="TimeZone">IANA time-zone id used to render local times for this event (e.g. <c>Europe/Amsterdam</c>).</param>
public sealed record TicketedEventCreationRequested(
    Guid CreationRequestId,
    Guid TeamId,
    string TeamSlug,
    string Slug,
    string Name,
    string WebsiteUrl,
    string BaseUrl,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string TimeZone) : IntegrationEvent;
