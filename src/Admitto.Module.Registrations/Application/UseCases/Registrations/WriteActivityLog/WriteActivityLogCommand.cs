using Amolenk.Admitto.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Module.Shared.Application.Messaging;

namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog;

internal sealed record WriteActivityLogCommand(
    RegistrationId RegistrationId,
    ActivityType ActivityType,
    DateTimeOffset OccurredAt,
    string? Metadata = null) : Command;
