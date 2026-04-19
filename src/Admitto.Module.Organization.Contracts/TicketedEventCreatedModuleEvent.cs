using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Organization.Contracts;

/// <summary>
/// Published when a ticketed event is created in the Organization module so that
/// other modules can initialise per-event state (e.g. the Registrations module
/// creates its <c>EventRegistrationPolicy</c>).
/// </summary>
public sealed record TicketedEventCreatedModuleEvent(Guid TicketedEventId) : ModuleEvent;
