namespace Amolenk.Admitto.Module.Registrations.Application.UseCases.Registrations.GetRegistrations;

public sealed record RegistrationListItemDto(
    Guid Id,
    string Email,
    IReadOnlyList<TicketSummaryDto> Tickets,
    DateTimeOffset CreatedAt);

public sealed record TicketSummaryDto(string Slug, string Name);
