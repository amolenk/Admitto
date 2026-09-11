using Amolenk.Admitto.Core.Registrations.Domain.Entities;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.CheckIn;

public sealed record CheckInResponse(
    CheckInOutcome Outcome,
    Guid? RegistrationId,
    string? Name,
    IReadOnlyList<CheckInTicketDto>? TicketSelections,
    DateTimeOffset? CheckedInAt)
{
    public static CheckInResponse Invalid(CheckInOutcome outcome) =>
        new(outcome, null, null, null, null);

    public static CheckInResponse ForRegistration(Registration registration, CheckInOutcome outcome) =>
        new(
            outcome,
            registration.Id.Value,
            $"{registration.FirstName.Value} {registration.LastName.Value}",
            registration.Tickets
                .Select(t => new CheckInTicketDto(t.Id.Value, t.Name.Value))
                .ToList(),
            registration.CheckedInAt);
}

public sealed record CheckInTicketDto(Guid Id, string Name);
