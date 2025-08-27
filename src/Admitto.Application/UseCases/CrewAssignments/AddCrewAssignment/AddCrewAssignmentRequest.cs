namespace Amolenk.Admitto.Application.UseCases.CrewAssignments.AddCrewAssignment;

public record AddCrewAssignmentRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails);

public record AdditionalDetailDto(string Name, string Value);
