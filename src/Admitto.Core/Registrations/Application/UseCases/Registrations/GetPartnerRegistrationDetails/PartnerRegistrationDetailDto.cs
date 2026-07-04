using Amolenk.Admitto.Core.Registrations.Contracts;

namespace Amolenk.Admitto.Core.Registrations.Application.UseCases.Registrations.GetPartnerRegistrationDetails;

public sealed record PartnerRegistrationDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    RegistrationStatus Status,
    IReadOnlyList<Guid> TicketTypeIds,
    IReadOnlyList<PartnerTicketDetailDto> Tickets,
    IReadOnlyDictionary<string, string> AdditionalDetails);

public sealed record PartnerTicketDetailDto(Guid Id, string Name);
