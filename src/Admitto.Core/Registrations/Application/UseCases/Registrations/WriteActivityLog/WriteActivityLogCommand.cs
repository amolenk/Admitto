using Amolenk.Admitto.Core.Registrations.Contracts.ValueObjects;
using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.WriteActivityLog;

internal sealed record WriteActivityLogCommand(
    TeamId TeamId,
    TicketedEventId EventId,
    RegistrationId RegistrationId,
    ActivityType ActivityType,
    DateTimeOffset OccurredAt,
    string? Metadata = null) : Command;
