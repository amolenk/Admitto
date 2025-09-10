namespace Amolenk.Admitto.Application.UseCases.Public.Register;

public record RegisterRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails,
    List<string> Tickets,
    string? VerificationToken = null);

public record AdditionalDetailDto(string Name, string Value);
