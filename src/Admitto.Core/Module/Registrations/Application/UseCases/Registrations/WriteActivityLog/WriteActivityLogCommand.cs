using Amolenk.Admitto.Core.Module.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Module.Registrations.Application.UseCases.Registrations.WriteActivityLog;

internal sealed record WriteActivityLogCommand(
    Guid RegistrationId,
    ActivityType ActivityType,
    DateTimeOffset OccurredAt,
    string? Metadata = null) : Command;
