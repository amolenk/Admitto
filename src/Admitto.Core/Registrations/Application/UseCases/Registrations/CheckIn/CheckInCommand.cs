using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

internal sealed record CheckInCommand(
    Guid TeamId,
    Guid EventId,
    string Credential) : Command<CheckInResponse>;
