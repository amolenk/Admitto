using Amolenk.Admitto.Core.Registrations.Domain.ValueObjects;
using Amolenk.Admitto.Core.Shared.Application.Messaging;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

internal sealed record CheckInCommand(
    Guid TeamId,
    Guid EventId,
    string Credential,
    CheckInSource Source = CheckInSource.Dashboard) : Command<CheckInResponse>;
