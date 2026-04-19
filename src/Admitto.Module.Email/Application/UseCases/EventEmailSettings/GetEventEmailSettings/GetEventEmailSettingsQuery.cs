using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Email.Application.UseCases.EventEmailSettings.GetEventEmailSettings;

internal sealed record GetEventEmailSettingsQuery(Guid TicketedEventId) : Query<EventEmailSettingsDto?>;
