namespace Amolenk.Admitto.Application.UseCases.Speakers.AddSpeakerEngagement;

public record AddSpeakerEngagementRequest(
    string Email,
    string FirstName,
    string LastName,
    List<AdditionalDetailDto> AdditionalDetails);

public record AdditionalDetailDto(string Name, string Value);
